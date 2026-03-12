namespace SuperAppModels.Models
{
    /// <summary>
    /// Self-contained node in kws.workspace_items.
    /// No entity_type/entity_id — name/description/color/icon stored directly in this table.
    /// </summary>
    public class KWorkspaceItemEntity : ITimestampEntity
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int? ParentId { get; set; }  // self-ref, null = root

        // Node data
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; } = "#F59E0B";
        public string? Icon { get; set; } = "📁";

        // Materialized Path
        public string PathIds { get; set; } = "/";
        public int PathDepth { get; set; } = 0;

        // Timestamps
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public KWorkspace KWorkspace { get; set; } = null!;
        public KWorkspaceItemEntity? Parent { get; set; }

        public KWorkspaceItemEntity()
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
