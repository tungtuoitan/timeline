using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace SuperAppServices.Services.K
{
    public class ClaudibleService : Interfaces.IClaudibleService
    {
        private readonly ChatClient _client;
        private readonly ILogger<ClaudibleService> _logger;

        public ClaudibleService(IConfiguration config, ILogger<ClaudibleService> logger)
        {
            _logger = logger;

            var apiKey  = config["Claudible:ApiKey"]  ?? throw new InvalidOperationException("Claudible:ApiKey is not configured.");
            var baseUrl = config["Claudible:BaseUrl"]  ?? "https://claudible.io/v1";
            var model   = config["Claudible:Model"]    ?? "claude-sonnet-4-5";

            var options = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
            _client = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options)
                          .GetChatClient(model);
        }

        public async Task<string> ChatAsync(string userMessage, string systemPrompt)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            _logger.LogDebug("Calling Claudible AI, prompt length: {Len}", userMessage.Length);
            var response = await _client.CompleteChatAsync(messages);
            return response.Value.Content[0].Text;
        }
    }
}
