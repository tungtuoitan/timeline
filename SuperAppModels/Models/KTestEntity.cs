namespace SuperAppModels.Models
{
    public class KTestEntity : ITimestampEntity
    {
        public int     Id          { get; set; }
        public int     KnowledgeId { get; set; }
        public int     UserId      { get; set; }
        public string  Title       { get; set; } = string.Empty;
        public int     Level       { get; set; } = 1;
        public string? Mode        { get; set; } = "standard";
        public string? Status      { get; set; } = "active";

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public KKnowledge                    Knowledge  { get; set; } = null!;
        public ICollection<KTestNodeEntity>  TestNodes  { get; set; } = [];
        public ICollection<KPointHistoryEntity> PointHistory { get; set; } = [];
    }
}
