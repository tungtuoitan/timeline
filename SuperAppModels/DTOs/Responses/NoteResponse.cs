namespace SuperAppModels.DTOs.Responses
{
    public class NoteResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsArchived { get; set; }
        public List<TagResponse> Tags { get; set; } = new List<TagResponse>();
        
        // Note: CreatedBy removed for security - sensitive data should not be exposed in API responses
    }
}
