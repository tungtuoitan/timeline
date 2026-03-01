namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Filter options for querying projects
    /// </summary>
    public class ProjectFilterOptions
    {
        /// <summary>
        /// User ID for filtering projects (required for data isolation)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Search text for name/description
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// Filter by multiple statuses (e.g., ["active", "completed"])
        /// </summary>
        public List<string>? StatusCodes { get; set; }

        /// <summary>
        /// Filter by deletedAt: "null" for active only, "notNull" for deleted only
        /// </summary>
        public string? DeletedAt { get; set; }

        /// <summary>
        /// Comma-separated project IDs for filtering specific projects
        /// </summary>
        public List<int>? Ids { get; set; }
    }
}
