using System;
using System.Threading.Tasks;
using quickLink.Helpers;
using quickLink.Models;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// A persisted AI conversation surfaced in the launcher. Selecting it reopens the
    /// chat. History is capped/pruned automatically, so no delete button is shown.
    /// </summary>
    public sealed class ChatHistoryItem : IListItem
    {
        private readonly ChatSession _session;

        public ChatHistoryItem(ChatSession session)
        {
            _session = session;
        }

        public string Id => _session.Id;

        public string DisplayTitle => string.IsNullOrWhiteSpace(_session.Title)
            ? $"Chat from {_session.CreatedUtc.ToLocalTime():MMM d}"
            : _session.Title;

        public string DisplayValue => FormatRelative(_session.UpdatedUtc);

        public string IconGlyph => "\uE8BD"; // Chat/comment glyph
        public string IconColor => "#FF6BCB";
        public bool ShowEditButton => false;
        public bool ShowDeleteButton => false;
        public bool SupportsAutocomplete => false;
        public string AutocompleteText => string.Empty;

        // Icon properties for XAML compatibility (mirrors InternalCommandItem)
        public bool HasFavicon => false;
        public string? FaviconUrl => null;
        public bool UseEmojiIcon => false;
        public string EmojiIcon => string.Empty;
        public bool UseGlyphIcon => true;
        public int[]? TitleHighlights { get; set; }

        public Task ExecuteAsync(IExecutionContext context)
        {
            context.OpenChatSession(Id);
            return Task.CompletedTask;
        }

        public bool MatchesSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return FuzzyMatcher.TryMatch(searchText, DisplayTitle, out _);
        }

        private static string FormatRelative(DateTime utc)
        {
            var local = utc.ToLocalTime();
            var delta = DateTime.Now - local;

            if (delta.TotalMinutes < 1) return "just now";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
            if (delta.TotalDays < 7) return $"{(int)delta.TotalDays}d ago";
            return local.ToString("MMM d");
        }
    }
}
