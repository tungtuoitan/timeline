namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Filter options for querying tasks
    /// </summary>
    public class TaskFilterOptions
    {
        /// <summary>
        /// User ID for filtering tasks via project ownership (required for data isolation)
        /// Tasks belong to user through their projects
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Comma-separated project IDs to filter tasks
        /// </summary>
        public List<int>? ProjectIds { get; set; }

        /// <summary>
        /// Search text for title/description
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// Filter by status (e.g., "open", "done")
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by priority (e.g., "low", "medium", "high")
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Filter by type (e.g., "task", "milestone")
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Filter by deletedAt: "null" for active only, "notNull" for deleted only
        /// </summary>
        public string? DeletedAt { get; set; }

        /// <summary>
        /// Comma-separated task IDs for filtering specific tasks
        /// </summary>
        public List<int>? Ids { get; set; }
    }
}
