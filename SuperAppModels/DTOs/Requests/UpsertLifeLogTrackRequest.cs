using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for creating or updating a LifeLog Track (upsert operation)
    /// If Id is 0, creates a new track. Otherwise, updates the existing track.
    /// </summary>
    public class UpsertLifeLogTrackRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        //[StringLength(255)]
        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isSensitive")]
        public bool IsSensitive { get; set; } = false;

        [StringLength(50)]
        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
