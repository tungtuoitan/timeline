namespace SuperAppServices.Interfaces
{
    /// <summary>One question to the model (Claude through the Anthropic API) and its text answer.</summary>
    public interface IAiChatService
    {
        Task<string> ChatAsync(string userMessage, string systemPrompt);
    }
}
