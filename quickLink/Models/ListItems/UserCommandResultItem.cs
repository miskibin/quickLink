using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// Actual result from a user-defined command (e.g., a file from /docs command)
    /// </summary>
    public class UserCommandResultItem : IListItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string FileDisplayName { get; set; } = string.Empty;
        public string FileIcon { get; set; } = string.Empty;
        public string ExecuteTemplate { get; set; } = string.Empty;
        public bool OpenInTerminal { get; set; } = false;

        public string DisplayTitle => FileDisplayName;
        public string DisplayValue => Path;
        public string IconGlyph => string.Empty; // We use emoji icon instead
        public string IconColor => "#FFFFFF";
        public bool ShowEditButton => false;
        public bool ShowDeleteButton => false;
        public bool SupportsAutocomplete => false;
        public string AutocompleteText => string.Empty;

        // For emoji-based icons
        public bool UseEmojiIcon => !string.IsNullOrEmpty(FileIcon);
        public string EmojiIcon => FileIcon;

        // Icon properties for XAML compatibility
        public bool HasFavicon => false;
        public string? FaviconUrl => null;
        public bool UseGlyphIcon => !UseEmojiIcon; // Show glyph only if no emoji

        public UserCommandResultItem() { }

        public UserCommandResultItem(string name, string path, string extension, string displayName, string icon, string executeTemplate, bool openInTerminal = false)
        {
            Name = name;
            Path = path;
            Extension = extension;
            FileDisplayName = displayName;
            FileIcon = icon;
            ExecuteTemplate = executeTemplate;
            OpenInTerminal = openInTerminal;
        }

        public async Task ExecuteAsync(IExecutionContext context)
        {
            var command = ExecuteTemplate
                .Replace("{item.path}", Path)
                .Replace("{item.name}", Name)
                .Replace("{item.extension}", Extension);

            System.Diagnostics.Debug.WriteLine($"UserCommandResultItem.ExecuteAsync: OpenInTerminal={OpenInTerminal}, Command={command}");

            if (OpenInTerminal)
            {
                System.Diagnostics.Debug.WriteLine("UserCommandResultItem.ExecuteAsync: Calling ExecuteCommandInTerminalAsync");
                await context.ExecuteCommandInTerminalAsync(command);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("UserCommandResultItem.ExecuteAsync: Calling ExecuteCommandAsync (silent)");
                await context.ExecuteCommandAsync(command);
            }

            context.HideWindow();
        }

        public bool MatchesSearch(string searchText)
        {
            // Already filtered by DirectoryCommandProvider
            return true;
        }
    }
}
