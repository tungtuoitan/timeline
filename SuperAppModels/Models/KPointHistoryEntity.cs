namespace SuperAppModels.Models
{
    /// <summary>
    /// Records the AI-assigned score for a single answer submission.
    /// One row per question per submission session.
    /// </summary>
    public class KPointHistoryEntity
    {
        public int     Id             { get; set; }
        /// <summary>The k.knowledge.id this answer belongs to. Null = orphaned question history.</summary>
        public int?    KnowledgeId    { get; set; }
        public int     UserId         { get; set; }
        /// <summary>The k.question.id this answer is for.</summary>
        public int?    QuestionId     { get; set; }
        /// <summary>The user's answer text.</summary>
        public string? AnswerText     { get; set; }
        /// <summary>Score assigned by AI: 0 (skipped/blank) or 1–5</summary>
        public int     Point          { get; set; } = 0;
        /// <summary>How long the user took to answer, in milliseconds.</summary>
        public int?    ResponseTimeMs { get; set; }
        public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
    }
}
