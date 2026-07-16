using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using quickLink.Constants;
using quickLink.Models;
using quickLink.Services.Helpers;

namespace quickLink.Services
{
    /// <summary>
    /// Persists AI conversations to chats.json as a rolling last-3 history. Older
    /// sessions are permanently dropped on save. Empty sessions are never persisted.
    /// </summary>
    public sealed class ChatHistoryService
    {
        // Rolling cap: only the most recent conversations are kept on disk.
        private const int MaxSessions = 3;

        private readonly string _chatsFilePath;
        private readonly SemaphoreSlim _fileLock;
        private readonly JsonSerializerOptions _jsonOptions;

        private List<ChatSession> _sessions;

        public ChatHistoryService()
        {
            _chatsFilePath = ServiceInitializer.GetDataFilePath(AppConstants.Files.ChatsFile);
            _fileLock = new SemaphoreSlim(1, 1);
            _jsonOptions = ServiceInitializer.GetJsonSerializerOptions();
            _sessions = new List<ChatSession>();
        }

        /// <summary>
        /// Sessions ordered newest first (by UpdatedUtc descending).
        /// </summary>
        public IReadOnlyList<ChatSession> Sessions => _sessions;

        public async Task LoadAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_chatsFilePath))
                {
                    _sessions = new List<ChatSession>();
                    return;
                }

                var json = await File.ReadAllTextAsync(_chatsFilePath);
                var sessions = JsonSerializer.Deserialize<List<ChatSession>>(json, _jsonOptions)
                    ?? new List<ChatSession>();

                _sessions = sessions
                    .OrderByDescending(s => s.UpdatedUtc)
                    .Take(MaxSessions)
                    .ToList();
            }
            catch
            {
                _sessions = new List<ChatSession>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Creates a fresh (unsaved) session. It is only persisted once it has messages.
        /// </summary>
        public ChatSession StartNew() => new ChatSession();

        public ChatSession? GetById(string id) => _sessions.FirstOrDefault(s => s.Id == id);

        /// <summary>
        /// Upserts the session, sorts newest first, enforces the rolling cap, and writes.
        /// Sessions with no messages are ignored.
        /// </summary>
        public async Task SaveSessionAsync(ChatSession session)
        {
            if (session == null || session.Messages.Count == 0)
                return;

            await _fileLock.WaitAsync();
            try
            {
                var index = _sessions.FindIndex(s => s.Id == session.Id);
                if (index >= 0)
                    _sessions[index] = session;
                else
                    _sessions.Add(session);

                _sessions = _sessions
                    .OrderByDescending(s => s.UpdatedUtc)
                    .Take(MaxSessions)
                    .ToList();

                await WriteAsync();
            }
            catch
            {
                // Silently fail - matches DataService behavior.
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task SetTitleAsync(string sessionId, string title)
        {
            await _fileLock.WaitAsync();
            try
            {
                var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
                if (session == null)
                    return;

                session.Title = title;
                await WriteAsync();
            }
            catch
            {
                // Silently fail.
            }
            finally
            {
                _fileLock.Release();
            }
        }

        // Caller must hold _fileLock.
        private async Task WriteAsync()
        {
            var json = JsonSerializer.Serialize(_sessions, _jsonOptions);
            await File.WriteAllTextAsync(_chatsFilePath, json);
        }
    }
}
