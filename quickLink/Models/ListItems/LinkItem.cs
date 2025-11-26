using System;
using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    public class LinkItem : IListItem, IEditableItem
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
                UpdateFavicon();
            }
        }

        public bool IsEncrypted { get; set; }
        public string? FaviconUrl { get; private set; }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "🔗 Link" : Title;
        public string DisplayValue => IsEncrypted ? "••••••••" : Value;
        public string IconGlyph => IsEncrypted ? "\uE72E" : "\uE774"; // Lock or Link icon
        public string IconColor => IsEncrypted ? "#FFB900" : "#3AADFF";
        public bool ShowEditButton => true;
        public bool ShowDeleteButton => true;
        public bool SupportsAutocomplete => false;
        public string AutocompleteText => string.Empty;
        public bool HasFavicon => !string.IsNullOrEmpty(FaviconUrl);

        // Icon properties for XAML compatibility
        public bool UseEmojiIcon => false;
        public string EmojiIcon => string.Empty;
        public bool UseGlyphIcon => !HasFavicon; // Show glyph only if no favicon

        public LinkItem()
        {
            UpdateFavicon();
        }

        public LinkItem(string title, string url, bool isEncrypted = false)
        {
            _title = title;
            _value = url;
            IsEncrypted = isEncrypted;
            UpdateFavicon();
        }

        private void UpdateFavicon()
        {
            if (!string.IsNullOrWhiteSpace(_value))
            {
                try
                {
                    var uri = new Uri(_value);
                    FaviconUrl = $"https://icons.duckduckgo.com/ip3/{uri.Host}.ico";
                }
                catch
                {
                    FaviconUrl = null;
                }
            }
        }

        public async Task ExecuteAsync(IExecutionContext context)
        {
            await context.OpenUrlAsync(Value);
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
