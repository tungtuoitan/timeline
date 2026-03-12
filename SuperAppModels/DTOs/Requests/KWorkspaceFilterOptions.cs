namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Filter options for K workspace tree queries
    /// </summary>
    public class KWorkspaceFilterOptions
    {
        /// <summary>User ID for filtering</summary>
        public int UserId { get; set; }

        /// <summary>
        /// Status codes to filter by (e.g. "active,archived")
        /// </summary>
        public List<string>? StatusCodes { get; set; }

        /// <summary>
        /// Deleted status: "null" = active only, "notNull" = deleted only, null = all
        /// </summary>
        public string? DeletedAt { get; set; }
    }
}
