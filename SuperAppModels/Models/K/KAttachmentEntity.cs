namespace SuperAppModels.Models
{
    public class KAttachmentEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "code";
        public string? Language { get; set; }
        public string? Content { get; set; }
        public int SortOrder { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class KAttachmentLinkEntity
    {
        public int Id { get; set; }
        public int AttachmentId { get; set; }
        /// <summary>"question" | "node"</summary>
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
