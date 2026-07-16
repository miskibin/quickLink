using System;
using System.Collections.Generic;

namespace quickLink.Models
{
    /// <summary>
    /// A single message inside a persisted <see cref="ChatSession"/>.
    /// </summary>
    public sealed class ChatMessageRecord
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// A persisted AI conversation. Stored in chats.json as part of a rolling
    /// last-3 history and surfaced in the launcher as a searchable item.
    /// </summary>
    public sealed class ChatSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public List<ChatMessageRecord> Messages { get; set; } = new();
    }
}
