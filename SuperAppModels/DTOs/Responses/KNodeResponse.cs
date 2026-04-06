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

        /// <summary>Workflow status — "draft" | null</summary>
        public string? StatusCode { get; set; }

        public string PathIds { get; set; } = "/";
        public int PathDepth { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
