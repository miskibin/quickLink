using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using quickLink.Models;

namespace quickLink.Services
{
    public sealed class DataService
    {
        private readonly string _dataFilePath;
        private readonly string _settingsFilePath;
        private readonly EncryptionService _encryptionService;
        private readonly SemaphoreSlim _fileLock;
        private readonly JsonSerializerOptions _jsonOptions;
        
        private List<ClipboardItem>? _cachedItems;

        public DataService()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataFolder, "QuickLink");
            
            Directory.CreateDirectory(appFolder);

            _dataFilePath = Path.Combine(appFolder, "data.json");
            _settingsFilePath = Path.Combine(appFolder, "settings.json");
            _encryptionService = new EncryptionService();
            _fileLock = new SemaphoreSlim(1, 1);
            _jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<List<ClipboardItem>> LoadItemsAsync()
        {
            // Return cached items if available
            if (_cachedItems != null)
            {
                return new List<ClipboardItem>(_cachedItems);
            }

            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    _cachedItems = new List<ClipboardItem>();
                    return new List<ClipboardItem>();
                }

                var json = await File.ReadAllTextAsync(_dataFilePath);
                var items = JsonSerializer.Deserialize<List<ClipboardItemDto>>(json, _jsonOptions) 
                    ?? new List<ClipboardItemDto>();
                
                _cachedItems = items.Select(MapDtoToItem).ToList();
                return new List<ClipboardItem>(_cachedItems);
            }
            catch
            {
                _cachedItems = new List<ClipboardItem>();
                return new List<ClipboardItem>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task SaveItemsAsync(List<ClipboardItem> items)
        {
            await _fileLock.WaitAsync();
            try
            {
                var dtos = items.Select(MapItemToDto).ToList();
                var json = JsonSerializer.Serialize(dtos, _jsonOptions);
                await File.WriteAllTextAsync(_dataFilePath, json);
                
                _cachedItems = new List<ClipboardItem>(items);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public Task AddItemAsync(ClipboardItem item, List<ClipboardItem> currentItems)
        {
            currentItems.Add(item);
            return SaveItemsAsync(currentItems);
        }

        public Task UpdateItemAsync(ClipboardItem oldItem, ClipboardItem newItem, List<ClipboardItem> currentItems)
        {
            var index = currentItems.IndexOf(oldItem);
            if (index >= 0)
            {
                currentItems[index] = newItem;
                return SaveItemsAsync(currentItems);
            }
            return Task.CompletedTask;
        }

        public Task DeleteItemAsync(ClipboardItem item, List<ClipboardItem> currentItems)
        {
            currentItems.Remove(item);
            return SaveItemsAsync(currentItems);
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch
            {
                // Silently fail - could add logging
            }
        }

        private ClipboardItem MapDtoToItem(ClipboardItemDto dto)
        {
            var value = dto.IsEncrypted 
                ? _encryptionService.Decrypt(dto.Value ?? string.Empty) 
                : dto.Value ?? string.Empty;

            return new ClipboardItem
            {
                Title = dto.Title ?? string.Empty,
                Value = value,
                IsEncrypted = dto.IsEncrypted,
                IsLink = dto.IsLink
            };
        }

        private ClipboardItemDto MapItemToDto(ClipboardItem item)
        {
            var value = item.IsEncrypted 
                ? _encryptionService.Encrypt(item.Value) 
                : item.Value;

            return new ClipboardItemDto
            {
                Title = item.Title,
                Value = value,
                IsEncrypted = item.IsEncrypted,
                IsLink = item.IsLink
            };
        }

        private sealed class ClipboardItemDto
        {
            public string? Title { get; set; }
            public string? Value { get; set; }
            public bool IsEncrypted { get; set; }
            public bool IsLink { get; set; }
        }
    }
}
