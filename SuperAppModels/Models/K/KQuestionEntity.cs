namespace SuperAppModels.Models
{
    /// <summary>
    /// A question — k.question table.
    /// NodeId = null means the question is "orphaned" (not linked to any node).
    /// </summary>
    public class KQuestionEntity : ITimestampEntity
    {
        public int    Id     { get; set; }
        public int?   NodeId { get; set; }   // nullable = orphan
        public string Name        { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool   IsActive    { get; set; } = true;
        public bool   IsDraft     { get; set; } = false;
        public int    SortOrder   { get; set; } = 0;

        // SRS (Spaced Repetition) state
        public int       SrsInterval     { get; set; } = 0;
        public double    SrsEaseFactor   { get; set; } = 2.5;
        public int       SrsRepetitions  { get; set; } = 0;
        public DateTime? SrsNextReviewAt { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public KNodeEntity? Node { get; set; }
    }
}
