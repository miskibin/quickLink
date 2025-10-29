using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using quickLink.Models;

namespace quickLink.Services
{
    public class DataService
  {
        private readonly string _dataFilePath;
    private readonly EncryptionService _encryptionService;
        private List<ClipboardItem> _cachedItems;

        public DataService()
     {
     var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var appFolder = Path.Combine(appDataFolder, "QuickLink");
          
 if (!Directory.Exists(appFolder))
      {
      Directory.CreateDirectory(appFolder);
   }

   _dataFilePath = Path.Combine(appFolder, "data.json");
       _encryptionService = new EncryptionService();
   _cachedItems = new List<ClipboardItem>();
        }

        public async Task<List<ClipboardItem>> LoadItemsAsync()
    {
   if (_cachedItems.Any())
     {
     return new List<ClipboardItem>(_cachedItems);
     }

       if (!File.Exists(_dataFilePath))
       {
                _cachedItems = new List<ClipboardItem>();
      return new List<ClipboardItem>();
}

 try
            {
  var json = await File.ReadAllTextAsync(_dataFilePath);
     var items = JsonSerializer.Deserialize<List<ClipboardItemDto>>(json) ?? new List<ClipboardItemDto>();
  
    _cachedItems = items.Select(dto => new ClipboardItem
        {
           Title = dto.Title ?? string.Empty,
       Value = dto.IsEncrypted ? _encryptionService.Decrypt(dto.Value ?? string.Empty) : dto.Value ?? string.Empty,
   IsEncrypted = dto.IsEncrypted,
      IsLink = dto.IsLink
 }).ToList();

                return new List<ClipboardItem>(_cachedItems);
    }
       catch
     {
 _cachedItems = new List<ClipboardItem>();
   return new List<ClipboardItem>();
    }
        }

        public async Task SaveItemsAsync(List<ClipboardItem> items)
     {
  try
       {
       var dtos = items.Select(item => new ClipboardItemDto
{
       Title = item.Title,
        Value = item.IsEncrypted ? _encryptionService.Encrypt(item.Value) : item.Value,
   IsEncrypted = item.IsEncrypted,
           IsLink = item.IsLink
}).ToList();

  var json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
 await File.WriteAllTextAsync(_dataFilePath, json);
  
_cachedItems = new List<ClipboardItem>(items);
        }
 catch
   {
      // Silently fail - could add logging here
          }
        }

     public async Task AddItemAsync(ClipboardItem item, List<ClipboardItem> currentItems)
        {
  currentItems.Add(item);
  await SaveItemsAsync(currentItems);
  }

   public async Task UpdateItemAsync(ClipboardItem oldItem, ClipboardItem newItem, List<ClipboardItem> currentItems)
    {
var index = currentItems.IndexOf(oldItem);
   if (index >= 0)
    {
     currentItems[index] = newItem;
 await SaveItemsAsync(currentItems);
        }
        }

  public async Task DeleteItemAsync(ClipboardItem item, List<ClipboardItem> currentItems)
        {
  currentItems.Remove(item);
  await SaveItemsAsync(currentItems);
        }

   // DTO for serialization
        private class ClipboardItemDto
   {
  public string? Title { get; set; }
   public string? Value { get; set; }
  public bool IsEncrypted { get; set; }
    public bool IsLink { get; set; }
        }
    }
}
