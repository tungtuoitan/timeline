namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Filter options for querying workspaces
    /// </summary>
    public class WsFilterOptions
    {
        /// <summary>
        /// User ID for filtering (optional)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Search text for filtering by name or description
        /// </summary>
        public string? SearchText { get; set; }

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

        /// <summary>
        /// Specific workspace IDs to retrieve (for restoring tabs)
        /// </summary>
        public List<int>? Ids { get; set; }
    }
}
