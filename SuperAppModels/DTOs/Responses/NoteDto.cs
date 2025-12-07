namespace SuperAppModels.DTOs.Responses
{
    public class NoteDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // ✅ Track if note is deleted
        public bool IsArchived { get; set; }
        public List<TagDto> Tags { get; set; } = new List<TagDto>();
        
        // Note: CreatedBy removed for security - sensitive data should not be exposed in API responses
    }
}
