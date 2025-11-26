using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// Simple user-created command that starts with ">"
    /// </summary>
    public class CommandItem : IListItem, IEditableItem
    {
        private string _title = string.Empty;
        private string _value = string.Empty;
        private string? _titleLower;
        private string? _valueLower;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                _titleLower = null; // Invalidate cache
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                _valueLower = null; // Invalidate cache
            }
        }

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
            _title = title;
            _value = command;
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

            // Cache lowercase values for repeated searches
            _titleLower ??= _title?.ToLowerInvariant() ?? string.Empty;
            _valueLower ??= _value?.ToLowerInvariant() ?? string.Empty;

            return _titleLower.Contains(searchText) || _valueLower.Contains(searchText);
        }
    }
}
