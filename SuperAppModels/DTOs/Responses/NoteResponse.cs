namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response model for note operations with computed tag array property
    /// </summary>
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

        /// <summary>
        /// Array of individual tags parsed from the Tags string property
        /// </summary>
        public string[] TagArray => 
            string.IsNullOrWhiteSpace(Tags) 
                ? Array.Empty<string>() 
                : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(tag => tag.Trim())
                      .ToArray();
    }
}
