namespace quickLink.Constants
{
    /// <summary>
    /// Centralized constants for the application
    /// </summary>
    public static class AppConstants
    {
        public static class Folders
        {
            public const string AppName = "QuickLink";
            public const string DataFolder = "QuickLink";
        }

        public static class Files
        {
            public const string CommandsFile = "commands.json";
            public const string DataFile = "data.json";
            public const string SettingsFile = "settings.json";
            public const string CrashLogFile = "crash.log";
        }

        public static class CommandPrefixes
        {
            public const string InternalPrefix = "internal:";
            public const string CommandPrefix = ">";
            public const string SearchPrefix = "search:";
            public const string UserCommandPrefix = "/";

            // Internal commands
            public const string AddCommand = "internal:add";
            public const string AddCommandAdvanced = "internal:addcommand";
            public const string SettingsCommand = "internal:settings";
            public const string ExitCommand = "internal:exit";
            public const string MarkdownCommand = "internal:markdown";
            public const string OpenLastConversationCommand = "internal:openlastconversation";
        }

        public static class MediaCommands
        {
            public const string Next = ">next";
            public const string Previous = ">prev";
            public const string PlayPause = ">playpause";
        }

        public static class DefaultSettings
        {
            public const string DefaultSearchUrl = "https://chatgpt.com/?q={query}";
            public const string QueryPlaceholder = "{query}";
            public const string ItemPathPlaceholder = "{item.path}";
            public const string ItemNamePlaceholder = "{item.name}";
            public const string ItemExtensionPlaceholder = "{item.extension}";
        }
    }
}
