namespace SuperAppModels.Models
{
    /// <summary>
    /// Workspace with hierarchical tree structure
    /// </summary>
    public class WorkspaceWithTree
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<WorkspaceTreeItem> Items { get; set; } = new List<WorkspaceTreeItem>();
    }
}
