using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// KnowledgeTree - Card entity (unified node + connection)
    /// Maps to kt.card table
    /// isDefinition = true  → định nghĩa khái niệm (keyword card)
    /// isDefinition = false → mối liên hệ giữa các card (relation card)
    /// </summary>
    public class KtCard : ITimestampEntity
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("knowledgeId")]
        public int KnowledgeId { get; set; }

        [JsonPropertyName("parentCardId")]
        public int? ParentCardId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isDefinition")]
        public bool IsDefinition { get; set; } = true;

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public KtKnowledge? Knowledge { get; set; }
        public KtCard? ParentCard { get; set; }
        public ICollection<KtCardLink> SourceLinks { get; set; } = new List<KtCardLink>();
    }
}
