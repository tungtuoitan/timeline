namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response model for note operations
    /// </summary>
    public class NoteResponse
    {
        /// <summary>
        /// Note unique identifier
        /// </summary>
        public int NoteId { get; set; }

        /// <summary>
        /// Note name/title
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Note description/content
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Comma-separated note tags
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Note type/category
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Email of the user who created the note
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// When the note was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the note was last updated (UTC)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Whether the note is archived
        /// </summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// Array of individual tags (parsed from Tags string)
        /// </summary>
        public string[] TagArray => 
            string.IsNullOrWhiteSpace(Tags) 
                ? Array.Empty<string>() 
                : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(tag => tag.Trim())
                      .ToArray();
    }
}
