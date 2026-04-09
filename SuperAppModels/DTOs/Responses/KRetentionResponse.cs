namespace SuperAppModels.DTOs.Responses
{
    public class KRetentionSummaryResponse
    {
        /// <summary>Average retention across all active questions (0–100)</summary>
        public double Average { get; set; }
        /// <summary>Total active questions considered</summary>
        public int TotalQuestions { get; set; }
        /// <summary>Per-test breakdown</summary>
        public List<KRetentionTestItem> Tests { get; set; } = [];
    }

    public class KRetentionTestItem
    {
        public int TestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public double Retention { get; set; }
        public int QuestionCount { get; set; }
    }

    // ── Retention Graph ──────────────────────────────────────────────────────

    public class KRetentionGraphResponse
    {
        /// <summary>Question metadata — index matches Retentions arrays in Days.</summary>
        public List<KRetentionGraphQuestion> Questions { get; set; } = [];
        /// <summary>One entry per day, oldest→newest.</summary>
        public List<KRetentionGraphDay> Days { get; set; } = [];
    }

    public class KRetentionGraphQuestion
    {
        public int    Id        { get; set; }
        public string Name      { get; set; } = string.Empty;
        public string TestTitle { get; set; } = string.Empty;
    }

    public class KRetentionGraphDay
    {
        public string Date    { get; set; } = string.Empty;
        public double Average { get; set; }
        /// <summary>Per-question retention, same index order as Questions.</summary>
        public List<double> Retentions { get; set; } = [];
    }
}
