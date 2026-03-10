using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class KtKnowledgeFilterOptions
    {
        public int UserId { get; set; }
        public string? SearchText { get; set; }
        public int? ParentId { get; set; }
        public bool RootsOnly { get; set; } = false;
        public string? DeletedAt { get; set; } // "null" | "notNull"
    }

    public class UpsertKtKnowledgeRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("parentId")]
        public int? ParentId { get; set; }

        [JsonPropertyName("title")]
        [Required, StringLength(255, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
