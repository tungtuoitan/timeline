namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Common filter options for grid data queries
    /// </summary>
    public class FilterOptions
    {
        /// <summary>
        /// Filter by status codes (e.g., ["active", "inactive"])
        /// </summary>
        public List<string>? StatusCodes { get; set; }

        /// <summary>
        /// Filter by deleted status ("null" for active only, "notNull" for deleted only)
        /// </summary>
        public string? DeletedAt { get; set; }

        /// <summary>
        /// Created date from filter
        /// </summary>
        public DateTime? CreatedFrom { get; set; }

        /// <summary>
        /// Created date to filter
        /// </summary>
        public DateTime? CreatedTo { get; set; }

        /// <summary>
        /// Updated date from filter
        /// </summary>
        public DateTime? UpdatedFrom { get; set; }

        /// <summary>
        /// Updated date to filter
        /// </summary>
        public DateTime? UpdatedTo { get; set; }

        /// <summary>
        /// Search text for filtering by name or description
        /// </summary>
        public string? SearchText { get; set; }
    }
}
