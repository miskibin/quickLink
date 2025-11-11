using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using quickLink.Models;
using quickLink.Models.ListItems;

namespace quickLink.Services
{
    /// <summary>
    /// Tracks item usage for intelligent search ranking
    /// </summary>
    public class UsageTrackingService
    {
        private readonly string _usageFilePath;
        private ItemUsageStats _stats;

        public UsageTrackingService()
        {
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuickLink");
            Directory.CreateDirectory(appDataFolder);
            _usageFilePath = Path.Combine(appDataFolder, "usage.json");
            _stats = new ItemUsageStats();
        }

        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(_usageFilePath))
                {
                    var json = await File.ReadAllTextAsync(_usageFilePath);
                    _stats = JsonSerializer.Deserialize<ItemUsageStats>(json) ?? new ItemUsageStats();
                }
            }
            catch
            {
                _stats = new ItemUsageStats();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_stats, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_usageFilePath, json);
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Record that an item was used (executed/copied)
        /// </summary>
        public async Task RecordUsageAsync(IListItem item)
        {
            var key = GetItemKey(item);
            if (string.IsNullOrEmpty(key))
                return;

            if (!_stats.Items.ContainsKey(key))
            {
                _stats.Items[key] = new UsageInfo();
            }

            _stats.Items[key].UseCount++;
            _stats.Items[key].LastUsed = DateTime.UtcNow;

            await SaveAsync();
        }

        /// <summary>
        /// Get use count for ranking
        /// </summary>
        public int GetUseCount(IListItem item)
        {
            var key = GetItemKey(item);
            if (string.IsNullOrEmpty(key))
                return 0;

            return _stats.Items.TryGetValue(key, out var info) ? info.UseCount : 0;
        }

        /// <summary>
        /// Calculate usage score for ranking (logarithmic to prevent over-prioritizing)
        /// </summary>
        public double GetUsageScore(IListItem item)
        {
            var useCount = GetUseCount(item);
            return Math.Log(useCount + 1, 2); // log base 2 for gradual ranking boost
        }

        private string GetItemKey(IListItem item)
        {
            // Use different keys based on item type to uniquely identify
            return item switch
            {
                IEditableItem editable => $"{editable.Title}|{editable.Value}",
                InternalCommandItem cmd => $"internal|{cmd.CommandValue}",
                UserCommandResultItem result => $"command|{result.Path}",
                CommandSuggestionItem suggestion => $"suggestion|{suggestion.CommandPrefix}",
                SearchSuggestionItem search => $"search|{search.SearchQuery}",
                _ => string.Empty
            };
        }
    }
}
