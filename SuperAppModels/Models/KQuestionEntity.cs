namespace SuperAppModels.Models
{
    /// <summary>
    /// A question belonging to a test — k.question table.
    /// Self-contained: name/description stored directly (no reference to k.node).
    /// </summary>
    public class KQuestionEntity : ITimestampEntity
    {
        public int    Id          { get; set; }
        public int    TestId      { get; set; }
        public string Name        { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool   IsActive    { get; set; } = true;
        public int    SortOrder   { get; set; } = 0;

        // SRS (Spaced Repetition) state
        public int       SrsInterval    { get; set; } = 0;
        public double    SrsEaseFactor  { get; set; } = 2.5;
        public int       SrsRepetitions { get; set; } = 0;
        public DateTime? SrsNextReviewAt { get; set; }

        public DateTime? CreatedAt  { get; set; }
        public DateTime? UpdatedAt  { get; set; }
        public DateTime? DeletedAt  { get; set; }

        // Navigation
        public KTestEntity Test { get; set; } = null!;
    }
}
