namespace SuperAppServices.Interfaces
{
    /// <summary>One question to the model (through OpenRouter) and its text answer.</summary>
    public interface IAiChatService
    {
        Task<string> ChatAsync(string userMessage, string systemPrompt);
    }
}
