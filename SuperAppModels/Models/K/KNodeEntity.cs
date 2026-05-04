namespace SuperAppModels.Models
{
    /// <summary>
    /// Self-contained node — k.node table.
    /// name/description/color/icon stored directly, no external entity tables.
    /// </summary>
    public class KNodeEntity : ITimestampEntity
    {
        public int Id { get; set; }
        public int KnowledgeId { get; set; }   // FK → k.knowledge.id
        public int? ParentId { get; set; }      // self-ref, null = root

        // Node data
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; } = "#F59E0B";
        public string? Icon { get; set; } = "📁";

        /// <summary>Workflow status — "draft" | null (active)</summary>
        public string? StatusCode { get; set; }

        // Materialized path
        public string PathIds { get; set; } = "/";
        public int PathDepth { get; set; } = 0;

        // Timestamps
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public KKnowledge Knowledge { get; set; } = null!;
        public KNodeEntity? Parent { get; set; }

        public KNodeEntity()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
