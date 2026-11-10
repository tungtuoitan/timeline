using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests.Reflect
{
    /// <summary>Save one Reflect row: created when the id is new, updated when it already belongs to the user.</summary>
    public class UpsertReflectEntryRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        [Required]
        [StringLength(36, MinimumLength = 1)]
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [StringLength(64)]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("think")]
        public string Think { get; set; } = "";

        [JsonPropertyName("real")]
        public string Real { get; set; } = "";
    }
}
