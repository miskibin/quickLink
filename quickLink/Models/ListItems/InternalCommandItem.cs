using System.Threading.Tasks;
using quickLink.Constants;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// Built-in commands (settings, exit, media controls, etc.)
    /// </summary>
    public class InternalCommandItem : IListItem
    {
        private string _title = string.Empty;
        private string _commandValue = string.Empty;
        private string? _titleLower;
        private string? _commandLower;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                _titleLower = null; // Invalidate cache
            }
        }

        public string CommandValue
        {
            get => _commandValue;
            set
            {
                _commandValue = value;
                _commandLower = null; // Invalidate cache
            }
        }

        public string DisplayTitle => Title;
        public string DisplayValue
        {
            get
            {
                // Strip prefix for display
                if (CommandValue.StartsWith(AppConstants.CommandPrefixes.InternalPrefix))
                    return CommandValue.Substring(AppConstants.CommandPrefixes.InternalPrefix.Length);
                if (CommandValue.StartsWith(">"))
                    return CommandValue.Substring(1).Trim();
                return CommandValue;
            }
        }

        public string IconGlyph => GetIconForCommand(CommandValue);
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
        public bool UseGlyphIcon => true; // Always use glyph for internal commands

        public InternalCommandItem() { }

        public InternalCommandItem(string title, string commandValue)
        {
            _title = title;
            _commandValue = commandValue;
        }

        private static string GetIconForCommand(string commandValue)
        {
            return commandValue switch
            {
                AppConstants.CommandPrefixes.AddCommand => "\uE710",      // Add icon
                AppConstants.CommandPrefixes.AddCommandAdvanced => "\uE710", // Add icon
                AppConstants.CommandPrefixes.SettingsCommand => "\uE713", // Settings gear
                AppConstants.CommandPrefixes.ExitCommand => "\uE711",     // Close/Exit
                AppConstants.MediaCommands.Next => "\uE893",              // Next track
                AppConstants.MediaCommands.Previous => "\uE892",          // Previous track
                AppConstants.MediaCommands.PlayPause => "\uE768",         // Play/Pause
                AppConstants.CommandPrefixes.MarkdownCommand => "\uE8A5", // Document icon
                AppConstants.CommandPrefixes.OpenLastConversationCommand => "\uE8F4", // Chat bubbles icon
                _ => "\uE8B7"                                             // Default bulleted list
            };
        }

        public async Task ExecuteAsync(IExecutionContext context)
        {
            switch (CommandValue)
            {
                case AppConstants.CommandPrefixes.AddCommand:
                    context.ShowEditPanel(null!);
                    break;

                case AppConstants.CommandPrefixes.AddCommandAdvanced:
                    context.ShowCommandPanel(null);
                    break;

                case AppConstants.CommandPrefixes.SettingsCommand:
                    context.ShowSettingsPanel();
                    break;

                case AppConstants.CommandPrefixes.ExitCommand:
                    context.ExitApplication();
                    break;

                case AppConstants.MediaCommands.Next:
                    await context.ExecuteMediaCommandAsync("next");
                    context.HideWindow();
                    break;

                case AppConstants.MediaCommands.Previous:
                    await context.ExecuteMediaCommandAsync("prev");
                    context.HideWindow();
                    break;

                case AppConstants.MediaCommands.PlayPause:
                    await context.ExecuteMediaCommandAsync("playpause");
                    context.HideWindow();
                    break;

                case AppConstants.CommandPrefixes.MarkdownCommand:
                    context.ShowMarkdownPanel();
                    break;

                case AppConstants.CommandPrefixes.OpenLastConversationCommand:
                    context.RestoreLastConversation();
                    break;
            }
        }

        public bool MatchesSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            // Cache lowercase values for repeated searches
            _titleLower ??= _title?.ToLowerInvariant() ?? string.Empty;
            _commandLower ??= _commandValue?.ToLowerInvariant() ?? string.Empty;

            return _titleLower.Contains(searchText) || _commandLower.Contains(searchText);
        }
    }
}
