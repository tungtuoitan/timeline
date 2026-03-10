using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// KnowledgeTree - Card link (many-to-many: relation card ↔ definition cards)
    /// Maps to kt.card_link table
    /// </summary>
    public class KtCardLink
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("sourceCardId")]
        public int SourceCardId { get; set; }

        [JsonPropertyName("targetCardId")]
        public int TargetCardId { get; set; }

        // Navigation
        public KtCard? SourceCard { get; set; }
        public KtCard? TargetCard { get; set; }
    }
}
