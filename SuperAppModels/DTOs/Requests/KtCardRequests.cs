using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class KtCardFilterOptions
    {
        public int UserId { get; set; }
        public int? KnowledgeId { get; set; }
        public string? SearchText { get; set; }
        public string? DeletedAt { get; set; } // "null" | "notNull"
    }

    public class UpsertKtCardRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("knowledgeId")]
        public int KnowledgeId { get; set; }

        [JsonPropertyName("parentCardId")]
        public int? ParentCardId { get; set; }

        [JsonPropertyName("keyword")]
        [StringLength(255)]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        [Required, StringLength(255, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isDefinition")]
        public bool IsDefinition { get; set; } = true;

        /// <summary>
        /// IDs của các definition card mà relation card này link đến.
        /// Chỉ dùng khi IsDefinition = false.
        /// </summary>
        [JsonPropertyName("linkedCardIds")]
        public List<int> LinkedCardIds { get; set; } = new();

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
