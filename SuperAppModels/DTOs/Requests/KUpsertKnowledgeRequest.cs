using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for creating or updating a knowledge base.
    /// If Id is 0 or null, creates a new knowledge. Otherwise, updates the existing one.
    /// </summary>
    public class KUpsertKnowledgeRequest
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        /// <summary>Set by controller from JWT claims</summary>
        [JsonIgnore]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("imageBase64")]
        public string? ImageBase64 { get; set; }
    }
}
