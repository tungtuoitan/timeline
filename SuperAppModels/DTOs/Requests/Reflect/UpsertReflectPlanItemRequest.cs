using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests.Reflect
{
    /// <summary>Save one planned task: created when the id is new, updated when it already belongs to the user.</summary>
    public class UpsertReflectPlanItemRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        [Required]
        [StringLength(36, MinimumLength = 1)]
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [Required]
        [JsonPropertyName("planDate")]
        public string PlanDate { get; set; } = "";

        [StringLength(400)]
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [StringLength(32)]
        [JsonPropertyName("status")]
        public string Status { get; set; } = "planned";

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; } = "";

        [JsonPropertyName("extra")]
        public string Extra { get; set; } = "{}";
    }
}
