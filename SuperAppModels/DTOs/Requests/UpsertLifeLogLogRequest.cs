using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for creating or updating a LifeLog Log entry (upsert operation)
    /// If Id is 0, creates a new log. Otherwise, updates the existing log.
    /// </summary>
    public class UpsertLifeLogLogRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        /// <summary>
        /// Log type: track | event | reflection | lesson | mistake | note | moment | progress
        /// </summary>
        [Required(ErrorMessage = "Type is required")]
        [StringLength(50)]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "note";

        [JsonPropertyName("trackId")]
        public int? TrackId { get; set; }

        [StringLength(255)]
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isSensitive")]
        public bool IsSensitive { get; set; } = false;

        [StringLength(255)]
        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("occurAt")]
        public DateTime? OccurAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
