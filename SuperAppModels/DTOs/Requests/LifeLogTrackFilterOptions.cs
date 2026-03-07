namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Filter options for LifeLog Track queries
    /// </summary>
    public class LifeLogTrackFilterOptions
    {
        public int UserId { get; set; }
        public string? SearchText { get; set; }
        public string? DeletedAt { get; set; } // "null" | "notNull"
        public List<int>? Ids { get; set; }
    }
}
