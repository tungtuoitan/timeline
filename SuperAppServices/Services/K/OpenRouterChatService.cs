using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SuperAppServices.Services.K
{
    /// <summary>
    /// Asks a model through OpenRouter (OpenAI-compatible chat API). Config: OpenRouter:ApiKey (required, comes from
    /// the environment / vault, never from a file in git), OpenRouter:Model, OpenRouter:BaseUrl, OpenRouter:MaxTokens.
    /// </summary>
    public class OpenRouterChatService : Interfaces.IAiChatService
    {
        private const string DefaultModel = "google/gemini-2.5-flash";
        private const string DefaultBaseUrl = "https://openrouter.ai/api/v1";

        private readonly HttpClient _http;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly ILogger<OpenRouterChatService> _logger;

        public OpenRouterChatService(IConfiguration config, ILogger<OpenRouterChatService> logger)
        {
            _logger = logger;
            var apiKey = config["OpenRouter:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("OpenRouter:ApiKey is not configured.");

            _model = config["OpenRouter:Model"] is { Length: > 0 } m ? m : DefaultModel;
            _maxTokens = int.TryParse(config["OpenRouter:MaxTokens"], out var t) && t > 0 ? t : 4096;
            var baseUrl = config["OpenRouter:BaseUrl"] is { Length: > 0 } b ? b : DefaultBaseUrl;

            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromMinutes(3) };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> ChatAsync(string userMessage, string systemPrompt)
        {
            var body = JsonSerializer.Serialize(new
            {
                model = _model,
                max_tokens = _maxTokens,
                temperature = 0,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                }
            });

            _logger.LogDebug("Calling OpenRouter ({Model}), prompt length: {Len}", _model, userMessage.Length);
            using var response = await _http.PostAsync("chat/completions", new StringContent(body, Encoding.UTF8, "application/json"));
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"OpenRouter request failed: {(int)response.StatusCode} {Truncate(json, 300)}");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
    }
}
