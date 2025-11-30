namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Filter options for querying notes
    /// </summary>
    public class NoteFilterOptions
    {
        /// <summary>
        /// User email for filtering (optional)
        /// </summary>
        public string? UserEmail { get; set; }

        /// <summary>
        /// User ID for filtering (optional)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Get all notes flag (admin only)
        /// </summary>
        public bool GetAll { get; set; }

        /// <summary>
        /// Search text for filtering by name or description
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// Tag IDs for filtering notes by tags
        /// </summary>
        public List<int>? TagIds { get; set; }

        /// <summary>
        /// Page number for pagination (default: 1)
        /// </summary>
        public int? PageNumber { get; set; }

        /// <summary>
        /// Page size for pagination (default: 20, max: 100)
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Sort by field (default: CreatedAt)
        /// </summary>
        public string SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// Sort order: asc or desc (default: desc)
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }
}
