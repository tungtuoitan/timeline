namespace SuperAppModels.DTOs.Responses
{
    public class NoteDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public string? StatusCode { get; set; } // Status code from standard_registries
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // ✅ Track if note is deleted

        // Note: Tags/Hashtags are handled separately via standard_registries, not included in NoteDTO
        // Note: CreatedBy removed for security - sensitive data should not be exposed in API responses
    }
}
