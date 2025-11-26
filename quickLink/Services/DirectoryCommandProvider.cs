using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using quickLink.Models;
using quickLink.Models.ListItems;

namespace quickLink.Services
{
    public sealed class DirectoryCommandProvider
    {
        // Cache for directory listings with TTL
        private readonly ConcurrentDictionary<string, (List<UserCommandResultItem> Items, DateTime CachedAt)> _cache = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30); // 30 second TTL

        private static string GetCacheKey(SourceConfig config, string executeTemplate)
        {
            return $"{config.Path}|{config.Glob}|{config.Recursive}|{executeTemplate}";
        }

        /// <summary>
        /// Lists files from a directory based on glob pattern and recursive option.
        /// Optimized for performance with lazy evaluation, caching, and limited results.
        /// </summary>
        public async Task<List<UserCommandResultItem>> GetItemsAsync(SourceConfig config, string executeTemplate, bool openInTerminal = false, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(config.Path) || !Directory.Exists(config.Path))
            {
                return new List<UserCommandResultItem>();
            }

            var cacheKey = GetCacheKey(config, executeTemplate);

            // Check cache first
            if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow - cached.CachedAt < CacheTtl)
            {
                return cached.Items.Take(maxResults).ToList();
            }

            return await Task.Run(() =>
            {
                try
                {
                    var items = new List<UserCommandResultItem>();
                    var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

                    // Add the glob pattern - if recursive, ensure pattern matches subdirectories
                    string pattern = config.Recursive && !config.Glob.Contains("/") && !config.Glob.Contains("**")
                        ? $"**/{config.Glob}"
                        : config.Glob;

                    matcher.AddInclude(pattern);

                    // Enumerate files with permission error handling
                    var allFiles = Directory.EnumerateFiles(config.Path, "*", new EnumerationOptions
                    {
                        RecurseSubdirectories = config.Recursive,
                        IgnoreInaccessible = true,
                        AttributesToSkip = FileAttributes.System
                    });

                    // Collect more items for caching (up to 200 for search capability)
                    const int maxCacheItems = 200;
                    foreach (var file in allFiles)
                    {
                        if (items.Count >= maxCacheItems)
                            break;

                        // Get relative path for matching
                        var relativePath = Path.GetRelativePath(config.Path, file);

                        // Check if file matches glob pattern
                        if (!matcher.Match(relativePath).HasMatches)
                            continue;

                        var fileInfo = new FileInfo(file);
                        items.Add(new UserCommandResultItem(
                            name: Path.GetFileNameWithoutExtension(file),
                            path: file,
                            extension: fileInfo.Extension,
                            displayName: Path.GetFileName(file),
                            icon: GetFileIcon(fileInfo.Extension),
                            executeTemplate: executeTemplate,
                            openInTerminal: openInTerminal
                        ));
                    }

                    var sortedItems = items.OrderBy(i => i.FileDisplayName).ToList();

                    // Update cache
                    _cache[cacheKey] = (sortedItems, DateTime.UtcNow);

                    return sortedItems.Take(maxResults).ToList();
                }
                catch
                {
                    return new List<UserCommandResultItem>();
                }
            });
        }

        /// <summary>
        /// Searches files by name in addition to glob pattern.
        /// Uses cached items when available for faster search.
        /// </summary>
        public async Task<List<UserCommandResultItem>> SearchItemsAsync(SourceConfig config, string executeTemplate, bool openInTerminal, string searchText, int maxResults = 50)
        {
            // Get items (will use cache if available)
            var allItems = await GetItemsAsync(config, executeTemplate, openInTerminal, 200);

            if (string.IsNullOrWhiteSpace(searchText))
                return allItems.Take(maxResults).ToList();

            var searchLower = searchText.ToLowerInvariant();

            return allItems
                .Where(item =>
                    item.FileDisplayName.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// Clears the cache for a specific directory or all directories
        /// </summary>
        public void ClearCache(string? directoryPath = null)
        {
            if (directoryPath == null)
            {
                _cache.Clear();
            }
            else
            {
                var keysToRemove = _cache.Keys.Where(k => k.StartsWith(directoryPath)).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        private static string GetFileIcon(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".md" or ".markdown" => "📄",
                ".txt" => "📃",
                ".pdf" => "📕",
                ".doc" or ".docx" => "📘",
                ".xls" or ".xlsx" => "📊",
                ".ppt" or ".pptx" => "📙",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                ".mp3" or ".wav" or ".flac" => "🎵",
                ".mp4" or ".avi" or ".mkv" => "🎬",
                ".zip" or ".rar" or ".7z" => "📦",
                ".exe" or ".msi" => "⚙️",
                ".cs" or ".js" or ".ts" or ".py" or ".java" or ".cpp" or ".c" or ".h" => "💻",
                ".json" or ".xml" or ".yaml" or ".yml" => "📋",
                ".html" or ".css" => "🌐",
                _ => "📄"
            };
        }
    }
}
