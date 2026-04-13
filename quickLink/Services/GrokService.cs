using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using quickLink.Models;

namespace quickLink.Services
{
    public sealed class GrokService
    {
        private static readonly Dictionary<AiProvider, ProviderConfig> Providers = new()
        {
            [AiProvider.XAI] = new("https://api.x.ai/v1/chat/completions", "grok-4-1-fast-non-reasoning"),
            [AiProvider.OpenAI] = new("https://api.openai.com/v1/chat/completions", "gpt-4o-mini"),
            [AiProvider.Claude] = new("https://api.anthropic.com/v1/messages", "claude-sonnet-4-20250514"),
            [AiProvider.Ollama] = new("http://localhost:11434/v1/chat/completions", "llama3"),
        };

        private const string SystemPrompt = "Answer directly and concisely. No greetings, no filler phrases like 'of course' or 'here's what you need'. Just provide the answer.";

        private readonly HttpClient _client = new();
        private readonly List<ChatMessage> _conversationHistory = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private AiProvider _provider = AiProvider.XAI;
        private string _modelOverride = string.Empty;

        public AiProvider Provider
        {
            get => _provider;
            set => _provider = value;
        }

        public string ModelOverride
        {
            get => _modelOverride;
            set => _modelOverride = value ?? string.Empty;
        }

        public string GetEffectiveModel()
        {
            if (!string.IsNullOrWhiteSpace(_modelOverride))
                return _modelOverride;
            return Providers[_provider].Model;
        }

        public static string GetDefaultModel(AiProvider provider) => Providers[provider].Model;

        public void ClearHistory() => _conversationHistory.Clear();

        public List<(string role, string content)> GetConversationHistory()
        {
            return _conversationHistory.ConvertAll(m => (m.Role, m.Content));
        }

        public void RestoreHistory(List<(string role, string content)> history)
        {
            _conversationHistory.Clear();
            foreach (var (role, content) in history)
            {
                _conversationHistory.Add(new ChatMessage { Role = role, Content = content });
            }
        }

        public async Task StreamResponseAsync(string apiKey, string userMessage, Func<string, Task> onChunk, CancellationToken ct = default)
        {
            // Ollama doesn't need API key
            if (string.IsNullOrEmpty(apiKey) && _provider != AiProvider.Ollama)
            {
                await onChunk("Error: API key not configured. Please set your API key in Settings.");
                return;
            }

            if (_provider == AiProvider.Claude)
            {
                await StreamClaudeResponseAsync(apiKey, userMessage, onChunk, ct);
                return;
            }

            await StreamOpenAICompatibleAsync(apiKey, userMessage, onChunk, ct);
        }

        private async Task StreamOpenAICompatibleAsync(string apiKey, string userMessage, Func<string, Task> onChunk, CancellationToken ct)
        {
            var config = Providers[_provider];

            if (_conversationHistory.Count == 0)
            {
                _conversationHistory.Add(new ChatMessage { Role = "system", Content = SystemPrompt });
            }

            _conversationHistory.Add(new ChatMessage { Role = "user", Content = userMessage });

            var payload = new
            {
                model = GetEffectiveModel(),
                messages = _conversationHistory,
                stream = true,
                temperature = 0.7,
                max_tokens = 500
            };

            var request = new HttpRequestMessage(HttpMethod.Post, config.Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
            }

            HttpResponseMessage response;
            try
            {
                response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (Exception ex)
            {
                await onChunk($"Error: {ex.Message}");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                await onChunk($"Error: {response.StatusCode} - {error}");
                return;
            }

            var fullResponse = new StringBuilder();
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

                var data = line[6..];
                if (data == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var delta = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("delta");

                    if (delta.TryGetProperty("content", out var content))
                    {
                        var text = content.GetString() ?? "";
                        fullResponse.Append(text);
                        await onChunk(text);
                    }
                }
                catch { }
            }

            _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = fullResponse.ToString() });
        }

        private async Task StreamClaudeResponseAsync(string apiKey, string userMessage, Func<string, Task> onChunk, CancellationToken ct)
        {
            var config = Providers[_provider];

            // Claude uses a different message format - system prompt is a top-level field
            var messages = new List<object>();
            foreach (var msg in _conversationHistory)
            {
                if (msg.Role == "system") continue;
                messages.Add(new { role = msg.Role, content = msg.Content });
            }
            messages.Add(new { role = "user", content = userMessage });

            if (_conversationHistory.Count == 0)
            {
                _conversationHistory.Add(new ChatMessage { Role = "system", Content = SystemPrompt });
            }
            _conversationHistory.Add(new ChatMessage { Role = "user", Content = userMessage });

            var payload = new
            {
                model = GetEffectiveModel(),
                system = SystemPrompt,
                messages,
                stream = true,
                max_tokens = 500
            };

            var request = new HttpRequestMessage(HttpMethod.Post, config.Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            HttpResponseMessage response;
            try
            {
                response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (Exception ex)
            {
                await onChunk($"Error: {ex.Message}");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                await onChunk($"Error: {response.StatusCode} - {error}");
                return;
            }

            var fullResponse = new StringBuilder();
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

                var data = line[6..];
                if (data == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    var type = root.GetProperty("type").GetString();

                    if (type == "content_block_delta")
                    {
                        var delta = root.GetProperty("delta");
                        if (delta.TryGetProperty("text", out var text))
                        {
                            var chunk = text.GetString() ?? "";
                            fullResponse.Append(chunk);
                            await onChunk(chunk);
                        }
                    }
                }
                catch { }
            }

            _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = fullResponse.ToString() });
        }

        private sealed class ChatMessage
        {
            public string Role { get; set; } = "";
            public string Content { get; set; } = "";
        }

        private sealed record ProviderConfig(string Endpoint, string Model);
    }
}
