using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    public class TextItem : IListItem, IEditableItem
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

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "📄 Text" : Title;
        public string DisplayValue => IsEncrypted ? "••••••••" : Value;
        public string IconGlyph => IsEncrypted ? "\uE72E" : "\uE8A5"; // Lock or Document
        public string IconColor => IsEncrypted ? "#FFB900" : "#A0A0A0";
        public bool ShowEditButton => true;
        public bool ShowDeleteButton => true;
        public bool SupportsAutocomplete => false;
        public string AutocompleteText => string.Empty;

        // Icon properties for XAML compatibility
        public bool HasFavicon => false;
        public string? FaviconUrl => null;
        public bool UseEmojiIcon => false;
        public string EmojiIcon => string.Empty;
        public bool UseGlyphIcon => true; // Always use glyph for text items

        public TextItem() { }

        public TextItem(string title, string value, bool isEncrypted = false)
        {
            _title = title;
            _value = value;
            IsEncrypted = isEncrypted;
        }

        public async Task ExecuteAsync(IExecutionContext context)
        {
            context.CopyToClipboard(Value);
            context.HideWindow();
            await Task.CompletedTask;
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
