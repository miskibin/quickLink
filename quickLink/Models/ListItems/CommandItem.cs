using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// Simple user-created command that starts with ">"
    /// </summary>
    public class CommandItem : IListItem, IEditableItem
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "⚡ Command" : Title;
        public string DisplayValue
        {
            get
            {
                if (IsEncrypted)
                    return "••••••••";

                // Strip the ">" prefix for display
                if (Value.StartsWith(">"))
                {
                    var trimmed = Value.Substring(1).Trim();
                    return string.IsNullOrWhiteSpace(trimmed) ? "..." : trimmed;
                }
                return Value;
            }
        }
        public string IconGlyph => IsEncrypted ? "\uE72E" : "\uE756"; // Lock or Terminal icon
        public string IconColor => IsEncrypted ? "#FFB900" : "#00CC6A";
        public bool ShowEditButton => true;
        public bool ShowDeleteButton => true;
        public bool SupportsAutocomplete => false;
        public string AutocompleteText => string.Empty;

        // Icon properties for XAML compatibility
        public bool HasFavicon => false;
        public string? FaviconUrl => null;
        public bool UseEmojiIcon => false;
        public string EmojiIcon => string.Empty;
        public bool UseGlyphIcon => true; // Always use glyph for command items

        public CommandItem() { }

        public CommandItem(string title, string command, bool isEncrypted = false)
        {
            Title = title;
            Value = command;
            IsEncrypted = isEncrypted;
        }

        public async Task ExecuteAsync(IExecutionContext context)
        {
            var command = Value.TrimStart('>').Trim();
            await context.ExecuteCommandAsync(command);
            context.HideWindow();
        }

        public bool MatchesSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            var search = searchText.ToLowerInvariant();
            return (!string.IsNullOrWhiteSpace(Title) && Title.ToLowerInvariant().Contains(search)) ||
                   (!string.IsNullOrWhiteSpace(Value) && Value.ToLowerInvariant().Contains(search));
        }
    }
}
