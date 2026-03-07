namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Filter options for LifeLog Log queries
    /// </summary>
    public class LifeLogLogFilterOptions
    {
        public int UserId { get; set; }
        public string? SearchText { get; set; }

        /// <summary>
        /// Filter by type: track|event|reflection|lesson|mistake|note|moment|progress
        /// Comma-separated for multi-select
        /// </summary>
        public List<string>? Types { get; set; }

        public int? TrackId { get; set; }

        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }

        public string? DeletedAt { get; set; } // "null" | "notNull"
        public List<int>? Ids { get; set; }
    }
}
