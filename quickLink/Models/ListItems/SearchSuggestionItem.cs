using System.Threading.Tasks;
using quickLink.Constants;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// Search suggestion item (appears when no results match)
    /// </summary>
    public class SearchSuggestionItem : IListItem
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string SearchUrl { get; set; } = AppConstants.DefaultSettings.DefaultSearchUrl;

        public string DisplayTitle => "Start conversation";
        public string DisplayValue => SearchQuery;
        public string IconGlyph => "\uE721"; // Search icon
        public string IconColor => "#FF6BCB";
        public bool ShowEditButton => false;
        public bool ShowDeleteButton => false;
        public bool SupportsAutocomplete => false;
        public string AutocompleteText => string.Empty;

        // Icon properties for XAML compatibility
        public bool HasFavicon => false;
        public string? FaviconUrl => null;
        public bool UseEmojiIcon => false;
        public string EmojiIcon => string.Empty;
        public bool UseGlyphIcon => true; // Always use glyph for search suggestions

        public SearchSuggestionItem() { }

        public SearchSuggestionItem(string query, string searchUrl)
        {
            SearchQuery = query;
            SearchUrl = searchUrl;
        }

        public async Task ExecuteAsync(IExecutionContext context)
        {
            var query = System.Uri.EscapeDataString(SearchQuery);
            var url = SearchUrl.Replace(AppConstants.DefaultSettings.QueryPlaceholder, query);
            await context.OpenUrlAsync(url);
            context.HideWindow();
        }

        public bool MatchesSearch(string searchText)
        {
            return true; // Always shows when no other results
        }
    }
}
