using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    public class TextItem : IListItem, IEditableItem
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
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
            Title = title;
            Value = value;
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

            var search = searchText.ToLowerInvariant();
            return (!string.IsNullOrWhiteSpace(Title) && Title.ToLowerInvariant().Contains(search)) ||
                   (!string.IsNullOrWhiteSpace(Value) && Value.ToLowerInvariant().Contains(search));
        }
    }
}
