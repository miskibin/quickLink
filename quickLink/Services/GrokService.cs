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
        private static readonly Dictionary<AiProvider, ProviderDefaults> Providers = new()
        {
            [AiProvider.XAI] = new("https://api.x.ai/v1/chat/completions", "grok-4-1-fast-non-reasoning"),
            [AiProvider.OpenAI] = new("https://api.openai.com/v1/chat/completions", "gpt-4o-mini"),
            [AiProvider.Claude] = new("https://api.anthropic.com/v1/messages", "claude-sonnet-4-20250514"),
            [AiProvider.Ollama] = new("http://localhost:11434/v1/chat/completions", "llama3"),
            // Custom is OpenAI-compatible; endpoint comes from the user-supplied base URL and there is no default model.
            [AiProvider.Custom] = new(string.Empty, string.Empty),
        };

        private const string SystemPrompt = "Answer directly and concisely. No greetings, no filler phrases like 'of course' or 'here's what you need'. Just provide the answer.";
        private const string TitlePrompt = "Generate a concise 3-6 word title for this conversation. Reply with only the title, no quotes or punctuation.";

        private readonly HttpClient _client = new();
        private readonly List<ChatMessage> _conversationHistory = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private AiProvider _provider = AiProvider.XAI;
        private string _modelOverride = string.Empty;
        private string _apiKey = string.Empty;
        private string _baseUrl = string.Empty;

        /// <summary>
        /// Pushes the full active provider configuration into the service.
        /// </summary>
        public void Configure(AiProvider provider, ProviderConfig config)
        {
            _provider = provider;
            _modelOverride = config?.Model ?? string.Empty;
            _apiKey = config?.ApiKey ?? string.Empty;
            _baseUrl = config?.BaseUrl ?? string.Empty;
        }

        public string GetEffectiveModel()
        {
            if (!string.IsNullOrWhiteSpace(_modelOverride))
                return _modelOverride;
            return Providers[_provider].Model;
        }

        private string GetEffectiveEndpoint()
        {
            return _provider == AiProvider.Custom ? _baseUrl : Providers[_provider].Endpoint;
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

        public async Task StreamResponseAsync(string userMessage, Func<string, Task> onChunk, CancellationToken ct = default)
        {
            // Ollama and Custom are local/self-hosted friendly and don't require an API key.
            var apiKeyOptional = _provider == AiProvider.Ollama || _provider == AiProvider.Custom;
            if (string.IsNullOrEmpty(_apiKey) && !apiKeyOptional)
            {
                await onChunk("Error: API key not configured. Please set your API key in Settings.");
                return;
            }

            if (_provider == AiProvider.Custom)
            {
                if (string.IsNullOrWhiteSpace(_baseUrl))
                {
                    await onChunk("Error: Base URL not configured for the custom provider. Set it in Settings.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(GetEffectiveModel()))
                {
                    await onChunk("Error: Model not configured for the custom provider. Set it in Settings.");
                    return;
                }
            }

            if (_provider == AiProvider.Claude)
            {
                await StreamClaudeResponseAsync(userMessage, onChunk, ct);
                return;
            }

            await StreamOpenAICompatibleAsync(userMessage, onChunk, ct);
        }

        private async Task StreamOpenAICompatibleAsync(string userMessage, Func<string, Task> onChunk, CancellationToken ct)
        {
            var endpoint = GetEffectiveEndpoint();

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

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
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

        private async Task StreamClaudeResponseAsync(string userMessage, Func<string, Task> onChunk, CancellationToken ct)
        {
            var endpoint = Providers[_provider].Endpoint;

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

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", _apiKey);
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

        /// <summary>
        /// Produces a short conversation title via a small non-streaming completion using the
        /// active provider config. Never touches <see cref="_conversationHistory"/>. Returns null
        /// on any failure or empty result so the caller can fall back to a truncated message.
        /// </summary>
        public async Task<string?> GenerateTitleAsync(string userMessage, string assistantReply, CancellationToken ct = default)
        {
            try
            {
                var apiKeyOptional = _provider == AiProvider.Ollama || _provider == AiProvider.Custom;
                if (string.IsNullOrEmpty(_apiKey) && !apiKeyOptional)
                    return null;

                if (_provider == AiProvider.Custom &&
                    (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(GetEffectiveModel())))
                    return null;

                var conversation = $"User: {userMessage}\nAssistant: {assistantReply}";

                return _provider == AiProvider.Claude
                    ? await GenerateTitleClaudeAsync(conversation, ct)
                    : await GenerateTitleOpenAICompatibleAsync(conversation, ct);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> GenerateTitleOpenAICompatibleAsync(string conversation, CancellationToken ct)
        {
            var payload = new
            {
                model = GetEffectiveModel(),
                messages = new[]
                {
                    new { role = "system", content = TitlePrompt },
                    new { role = "user", content = conversation }
                },
                stream = false,
                temperature = 0.3,
                max_tokens = 20
            };

            var request = new HttpRequestMessage(HttpMethod.Post, GetEffectiveEndpoint())
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(_apiKey))
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return CleanTitle(content);
        }

        private async Task<string?> GenerateTitleClaudeAsync(string conversation, CancellationToken ct)
        {
            var payload = new
            {
                model = GetEffectiveModel(),
                system = TitlePrompt,
                messages = new[] { new { role = "user", content = conversation } },
                max_tokens = 20
            };

            var request = new HttpRequestMessage(HttpMethod.Post, Providers[_provider].Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return CleanTitle(content);
        }

        private static string? CleanTitle(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var title = raw.Trim().Trim('"').Trim();
            return string.IsNullOrWhiteSpace(title) ? null : title;
        }

        private sealed class ChatMessage
        {
            public string Role { get; set; } = "";
            public string Content { get; set; } = "";
        }

        private sealed record ProviderDefaults(string Endpoint, string Model);
    }
}
