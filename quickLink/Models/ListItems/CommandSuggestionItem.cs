using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// Suggestion to autocomplete a user-defined command (shows when typing "/")
    /// </summary>
    public class CommandSuggestionItem : IListItem
    {
        public string CommandPrefix { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public UserCommand? RelatedCommand { get; set; }

        public string DisplayTitle => $"{CommandPrefix} - {Description}";
        public string DisplayValue => Description;
        public string IconGlyph => string.Empty; // We use emoji icon
        public string IconColor => "#FFFFFF";
        public bool ShowEditButton => true; // Can edit the command definition
        public bool ShowDeleteButton => true; // Can delete the command
        public bool SupportsAutocomplete => true;
        public string AutocompleteText => CommandPrefix + " ";

        // For emoji-based icons
        public bool UseEmojiIcon => !string.IsNullOrEmpty(Icon);
        public string EmojiIcon => Icon;

        // Icon properties for XAML compatibility
        public bool HasFavicon => false;
        public string? FaviconUrl => null;
        public bool UseGlyphIcon => !UseEmojiIcon; // Show glyph only if no emoji

        public CommandSuggestionItem() { }

        public CommandSuggestionItem(UserCommand command)
        {
            CommandPrefix = command.Prefix;
            Description = $"{command.Source} command";
            Icon = command.IconDisplay;
            RelatedCommand = command;
        }

        public async Task ExecuteAsync(IExecutionContext context)
        {
            // This should not be called - autocomplete should handle it
            // But if it is called, do nothing
            await Task.CompletedTask;
        }

        public bool MatchesSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            var search = searchText.ToLowerInvariant();
            return CommandPrefix.ToLowerInvariant().Contains(search) ||
                   Description.ToLowerInvariant().Contains(search);
        }
    }
}
