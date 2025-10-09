namespace SuperAppModels.Models
{
    /// <summary>
    /// Model for filtering notes based on various criteria
    /// </summary>
    public class FilterModel
    {
        public string Parent { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string RepeatType { get; set; } = string.Empty;

        public string IsUpdatedToday { get; set; } = string.Empty;
    }
}