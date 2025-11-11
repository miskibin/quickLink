using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using quickLink.Constants;
using quickLink.Models;
using quickLink.Models.ListItems;
using quickLink.Services.Helpers;

namespace quickLink.Services
{
    public sealed class DataService
    {
        private readonly string _dataFilePath;
        private readonly string _settingsFilePath;
        private readonly EncryptionService _encryptionService;
        private readonly SemaphoreSlim _fileLock;
        private readonly JsonSerializerOptions _jsonOptions;

        private List<IListItem>? _cachedItems;

        public DataService()
        {
            _dataFilePath = ServiceInitializer.GetDataFilePath(AppConstants.Files.DataFile);
            _settingsFilePath = ServiceInitializer.GetDataFilePath(AppConstants.Files.SettingsFile);
            _encryptionService = new EncryptionService();
            _fileLock = new SemaphoreSlim(1, 1);
            _jsonOptions = ServiceInitializer.GetJsonSerializerOptions();
        }

        public async Task<List<IListItem>> LoadItemsAsync()
        {
            // Return cached items if available
            if (_cachedItems != null)
            {
                return new List<IListItem>(_cachedItems);
            }

            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    _cachedItems = new List<IListItem>();
                    return new List<IListItem>();
                }

                var json = await File.ReadAllTextAsync(_dataFilePath);
                var items = JsonSerializer.Deserialize<List<ItemDto>>(json, _jsonOptions)
                    ?? new List<ItemDto>();

                _cachedItems = items.Select(MapDtoToItem).ToList();
                return new List<IListItem>(_cachedItems);
            }
            catch
            {
                _cachedItems = new List<IListItem>();
                return new List<IListItem>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task SaveItemsAsync(List<IListItem> items)
        {
            await _fileLock.WaitAsync();
            try
            {
                var dtos = items
                    .Where(item => item is IEditableItem) // Only save editable items
                    .Select(MapItemToDto)
                    .ToList();
                var json = JsonSerializer.Serialize(dtos, _jsonOptions);
                await File.WriteAllTextAsync(_dataFilePath, json);

                _cachedItems = new List<IListItem>(items.Where(item => item is IEditableItem));
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public Task AddItemAsync(IEditableItem item, List<IListItem> currentItems)
        {
            var listItem = item as IListItem;
            if (listItem != null)
            {
                currentItems.Add(listItem);
            }
            return SaveItemsAsync(currentItems);
        }

        public Task UpdateItemAsync(IListItem oldItem, IEditableItem newItem, List<IListItem> currentItems)
        {
            var index = currentItems.IndexOf(oldItem);
            if (index >= 0 && newItem is IListItem newListItem)
            {
                currentItems[index] = newListItem;
                return SaveItemsAsync(currentItems);
            }
            return Task.CompletedTask;
        }

        public Task DeleteItemAsync(IListItem item, List<IListItem> currentItems)
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

        private IListItem MapDtoToItem(ItemDto dto)
        {
            var value = dto.IsEncrypted
                ? _encryptionService.Decrypt(dto.Value ?? string.Empty)
                : dto.Value ?? string.Empty;

            // Detect type based on value patterns
            var isLink = !string.IsNullOrWhiteSpace(value) &&
                (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

            var isCommand = !string.IsNullOrWhiteSpace(value) && value.StartsWith(">");

            if (isLink)
            {
                return new LinkItem(dto.Title ?? string.Empty, value, dto.IsEncrypted);
            }
            else if (isCommand)
            {
                return new CommandItem(dto.Title ?? string.Empty, value, dto.IsEncrypted);
            }
            else
            {
                return new TextItem(dto.Title ?? string.Empty, value, dto.IsEncrypted);
            }
        }

        private ItemDto MapItemToDto(IListItem item)
        {
            if (item is not IEditableItem editableItem)
            {
                throw new InvalidOperationException("Can only save editable items");
            }

            var value = editableItem.IsEncrypted
                ? _encryptionService.Encrypt(editableItem.Value)
                : editableItem.Value;

            return new ItemDto
            {
                Title = editableItem.Title,
                Value = value,
                IsEncrypted = editableItem.IsEncrypted
            };
        }

        private sealed class ItemDto
        {
            public string? Title { get; set; }
            public string? Value { get; set; }
            public bool IsEncrypted { get; set; }
        }
    }
}
