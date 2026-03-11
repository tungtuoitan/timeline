namespace SuperAppModels.Models
{
    /// <summary>
    /// Workspace with hierarchical tree structure
    /// </summary>
    public class KWorkspaceWithTree
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
         public List<KWorkspaceItem> Items { get; set; } = new List<KWorkspaceItem>();
    }
}
