using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// KnowledgeTree - Knowledge entity
    /// Maps to kt.knowledge table
    /// </summary>
    public class KtKnowledge : ITimestampEntity
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("parentId")]
        public int? ParentId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public KtKnowledge? Parent { get; set; }
        public ICollection<KtKnowledge> Children { get; set; } = new List<KtKnowledge>();
    }
}
