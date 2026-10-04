namespace SuperAppModels.DTOs.Requests.DailyLog
{
    /// <summary>
    /// Filter options for querying daily logs.
    /// </summary>
    public class DailyLogFilterOptions
    {
        public int UserId { get; set; }

        /// <summary>Inclusive lower bound (yyyy-MM-dd). Ignored if null.</summary>
        public DateOnly? FromDate { get; set; }

        /// <summary>Inclusive upper bound (yyyy-MM-dd). Ignored if null.</summary>
        public DateOnly? ToDate { get; set; }

        /// <summary>"null" for active only, "notNull" for deleted only, null for all.</summary>
        public string? DeletedAt { get; set; }
    }
}
