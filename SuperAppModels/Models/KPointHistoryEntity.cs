namespace SuperAppModels.Models
{
    /// <summary>
    /// Records the AI-assigned score for a single answer submission.
    /// One row per node per test submission.
    /// </summary>
    public class KPointHistoryEntity
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public int UserId { get; set; }
        /// <summary>The question node this answer is for.</summary>
        public int? NodeId { get; set; }
        /// <summary>The user's answer text.</summary>
        public string? AnswerText { get; set; }
        /// <summary>Score assigned by AI: 0 (skipped/blank) or 1–5</summary>
        public int Point { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public KTestEntity Test { get; set; } = null!;
    }
}
