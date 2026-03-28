namespace SuperAppServices.Interfaces
{
    public interface IClaudibleService
    {
        Task<string> ChatAsync(string userMessage, string systemPrompt);
    }
}
