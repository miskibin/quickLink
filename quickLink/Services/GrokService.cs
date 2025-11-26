using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace quickLink.Services
{
    public sealed class GrokService
    {
        private const string Endpoint = "https://api.x.ai/v1/chat/completions";
        private const string Model = "grok-4-1-fast-non-reasoning";
        
        private readonly HttpClient _client = new();
        private readonly List<ChatMessage> _conversationHistory = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public void ClearHistory() => _conversationHistory.Clear();

        public async Task StreamResponseAsync(string apiKey, string userMessage, Func<string, Task> onChunk, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                await onChunk("Error: API key not configured. Please set your API key in Settings.");
                return;
            }

            _conversationHistory.Add(new ChatMessage { Role = "user", Content = userMessage });

            var payload = new
            {
                model = Model,
                messages = _conversationHistory,
                stream = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

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
                catch { /* Skip malformed chunks */ }
            }

            _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = fullResponse.ToString() });
        }

        private sealed class ChatMessage
        {
            public string Role { get; set; } = "";
            public string Content { get; set; } = "";
        }
    }
}
