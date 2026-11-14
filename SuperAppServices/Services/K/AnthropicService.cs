using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SuperAppServices.Services.K
{
    /// <summary>
    /// Asks Claude through the official Anthropic Messages API (https://api.anthropic.com/v1/messages).
    /// Config: Anthropic:ApiKey (required, comes from the environment / vault, never from a file in git),
    /// Anthropic:Model, Anthropic:BaseUrl, Anthropic:MaxTokens.
    /// </summary>
    public class AnthropicService : Interfaces.IAiChatService
    {
        private const string DefaultModel = "claude-sonnet-5-5";
        private const string DefaultBaseUrl = "https://api.anthropic.com";
        private const string ApiVersion = "2023-06-01";

        private readonly HttpClient _http;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly ILogger<AnthropicService> _logger;

        public AnthropicService(IConfiguration config, ILogger<AnthropicService> logger)
        {
            _logger = logger;
            var apiKey = config["Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("Anthropic:ApiKey is not configured.");

            _model = config["Anthropic:Model"] is { Length: > 0 } m ? m : DefaultModel;
            _maxTokens = int.TryParse(config["Anthropic:MaxTokens"], out var t) && t > 0 ? t : 4096;
            var baseUrl = config["Anthropic:BaseUrl"] is { Length: > 0 } b ? b : DefaultBaseUrl;

            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromMinutes(3) };
            _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> ChatAsync(string userMessage, string systemPrompt)
        {
            var body = JsonSerializer.Serialize(new
            {
                model = _model,
                max_tokens = _maxTokens,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userMessage } }
            });

            _logger.LogDebug("Calling Anthropic ({Model}), prompt length: {Len}", _model, userMessage.Length);
            using var response = await _http.PostAsync("v1/messages", new StringContent(body, Encoding.UTF8, "application/json"));
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Anthropic request failed: {(int)response.StatusCode} {Truncate(json, 300)}");

            using var doc = JsonDocument.Parse(json);
            var text = new StringBuilder();
            foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
                if (block.TryGetProperty("type", out var type) && type.GetString() == "text")
                    text.Append(block.GetProperty("text").GetString());
            return text.ToString();
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
    }
}
