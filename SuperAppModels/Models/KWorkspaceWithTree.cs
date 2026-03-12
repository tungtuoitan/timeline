namespace SuperAppModels.Models
{
    /// <summary>
    /// Workspace with flat list of self-contained nodes (kws.workspace_items)
    /// </summary>
    public class KWorkspaceWithTree
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<KWorkspaceItemEntity> Items { get; set; } = new List<KWorkspaceItemEntity>();
    }
}
