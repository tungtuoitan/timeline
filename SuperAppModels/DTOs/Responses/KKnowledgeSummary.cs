namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Lightweight knowledge summary for list endpoints
    /// </summary>
    public class KKnowledgeSummary
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StatusCode { get; set; }
        public string? ImageBase64 { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
