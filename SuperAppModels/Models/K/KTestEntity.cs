namespace SuperAppModels.Models
{
    public class KTestEntity : ITimestampEntity
    {
        public int     Id          { get; set; }
        public int     KnowledgeId { get; set; }
        public int     UserId      { get; set; }
        /// <summary>The entity node this test belongs to (k.node.id). Null = not tied to a node.</summary>
        public int?    NodeId      { get; set; }
        public string  Title       { get; set; } = string.Empty;
        public int     Level       { get; set; } = 1;
        public string? Mode        { get; set; } = "standard";
        /// <summary>inactive | learning | mastered</summary>
        public string? Status      { get; set; } = "inactive";
        public int     SortOrder   { get; set; } = 0;

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public KKnowledge                       Knowledge    { get; set; } = null!;
        public ICollection<KQuestionEntity>     Questions    { get; set; } = [];
        public ICollection<KPointHistoryEntity> PointHistory { get; set; } = [];
    }
}
