using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using quickLink.Models;

namespace quickLink.Services
{
    public sealed class DirectoryCommandProvider
    {
        /// <summary>
        /// Lists files from a directory based on glob pattern and recursive option.
        /// Optimized for performance with lazy evaluation and limited results.
        /// </summary>
        public async Task<List<CommandResultItem>> GetItemsAsync(SourceConfig config, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(config.Path) || !Directory.Exists(config.Path))
            {
                return new List<CommandResultItem>();
            }

            return await Task.Run(() =>
            {
                try
                {
                    var items = new List<CommandResultItem>();
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
                    
                    foreach (var file in allFiles)
                    {
                        if (items.Count >= maxResults)
                            break;
                        
                        // Get relative path for matching
                        var relativePath = Path.GetRelativePath(config.Path, file);
                        
                        // Check if file matches glob pattern
                        if (!matcher.Match(relativePath).HasMatches)
                            continue;
                        
                        var fileInfo = new FileInfo(file);
                        items.Add(new CommandResultItem
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            Path = file,
                            Extension = fileInfo.Extension,
                            DisplayName = Path.GetFileName(file),
                            IconDisplay = GetFileIcon(fileInfo.Extension)
                        });
                    }
                    
                    return items.OrderBy(i => i.DisplayName).ToList();
                }
                catch
                {
                    return new List<CommandResultItem>();
                }
            });
        }

        /// <summary>
        /// Searches files by name in addition to glob pattern.
        /// </summary>
        public async Task<List<CommandResultItem>> SearchItemsAsync(SourceConfig config, string searchText, int maxResults = 50)
        {
            var allItems = await GetItemsAsync(config, maxResults * 2);
            
            if (string.IsNullOrWhiteSpace(searchText))
                return allItems.Take(maxResults).ToList();
            
            var searchLower = searchText.ToLowerInvariant();
            
            return allItems
                .Where(item => 
                    item.DisplayName.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
                .Take(maxResults)
                .ToList();
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
