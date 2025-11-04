using System;
using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    public class LinkItem : IListItem, IEditableItem
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
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
            Title = title;
            Value = url;
            IsEncrypted = isEncrypted;
            UpdateFavicon();
        }

        private void UpdateFavicon()
        {
            if (!string.IsNullOrWhiteSpace(Value))
            {
                try
                {
                    var uri = new Uri(Value);
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

            var search = searchText.ToLowerInvariant();
            return (!string.IsNullOrWhiteSpace(Title) && Title.ToLowerInvariant().Contains(search)) ||
                   (!string.IsNullOrWhiteSpace(Value) && Value.ToLowerInvariant().Contains(search));
        }
    }
}
