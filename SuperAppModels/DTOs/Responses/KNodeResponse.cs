namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Flat node response — maps to k.node table row
    /// </summary>
    public class KNodeResponse
    {
        public int Id { get; set; }
        public int KnowledgeId { get; set; }
        public int? ParentId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }

        public string PathIds { get; set; } = "/";
        public int PathDepth { get; set; }

        // Type — "draft" | "shortcut"
        public string TypeCode { get; set; } = "draft";

        // Shortcut: trỏ về node gốc (null = node thường)
        public int? RefTargetId { get; set; }
        public int? RefTargetKnowledgeId { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Với shortcut: resolved từ node gốc (null = gốc đang sống, non-null = gốc đã bị xóa).
        /// Với node thường: deleted_at của chính nó.
        /// </summary>
        public DateTime? DeletedAt { get; set; }
    }
}
