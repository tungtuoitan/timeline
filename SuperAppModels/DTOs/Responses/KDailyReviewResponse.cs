namespace SuperAppModels.DTOs.Responses
{
    /// <summary>Summary of a knowledge in the daily review queue.</summary>
    public class KDailyQueueItemResponse
    {
        public int    KnowledgeId   { get; set; }
        public string KnowledgeName { get; set; } = string.Empty;
        /// <summary>Questions due for review (next_review_at &lt;= now)</summary>
        public int DueCount    { get; set; }
        /// <summary>New questions (never reviewed, next_review_at is null)</summary>
        public int NewCount    { get; set; }
        /// <summary>Total active questions</summary>
        public int ActiveCount { get; set; }
        /// <summary>Questions with statusCode = 'draft'</summary>
        public int DraftCount  { get; set; }
    }

    /// <summary>A question in a daily review session.</summary>
    public class KDailySessionQuestionResponse
    {
        public int     Id       { get; set; }
        public string  Question { get; set; } = string.Empty;
        public string? Answer   { get; set; }
        /// <summary>Owning node name — used by knowledge-wide review screens.</summary>
        public string? NodeName { get; set; }
        /// <summary>Seconds from now until next review for each score 1–5.</summary>
        public Dictionary<int, long> PreviewIntervalSeconds { get; set; } = new();
    }
}
