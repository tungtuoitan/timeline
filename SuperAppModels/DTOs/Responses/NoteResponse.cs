namespace SuperAppModels.DTOs.Responses
{
    public class NoteResponse
    {
        public int NoteId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public string? Type { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsArchived { get; set; }
    }
}
