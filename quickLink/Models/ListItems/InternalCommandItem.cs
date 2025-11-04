using System.Threading.Tasks;
using quickLink.Constants;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// Built-in commands (settings, exit, media controls, etc.)
    /// </summary>
    public class InternalCommandItem : IListItem
    {
        public string Title { get; set; } = string.Empty;
        public string CommandValue { get; set; } = string.Empty;

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
            Title = title;
            CommandValue = commandValue;
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
            }
        }

        public bool MatchesSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            var search = searchText.ToLowerInvariant();
            return (!string.IsNullOrWhiteSpace(Title) && Title.ToLowerInvariant().Contains(search)) ||
                   (!string.IsNullOrWhiteSpace(CommandValue) && CommandValue.ToLowerInvariant().Contains(search));
        }
    }
}
