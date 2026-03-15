namespace SuperAppModels.Models
{
    /// <summary>
    /// Knowledge base with flat node list (used as intermediate repository result)
    /// </summary>
    public class KKnowledgeWithTree
    {
        public int KnowledgeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<KNodeEntity> Nodes { get; set; } = new();

        /// <summary>
        /// Shortcut target nodes keyed by their ID.
        /// Loaded in one batch query (PK lookup) — includes soft-deleted targets
        /// so resolved deleted_at can be propagated to shortcut nodes.
        /// </summary>
        public Dictionary<int, KNodeEntity> ShortcutTargets { get; set; } = new();
    }
}
