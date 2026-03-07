using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// LifeLog Log entity - a single log entry (free-form or track-triggered)
    /// Maps to log.log table
    /// </summary>
    public class LifeLogLog : ITimestampEntity
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        /// <summary>
        /// Log type: track | event | reflection | lesson | mistake | note | moment | progress
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "note";

        /// <summary>
        /// Optional reference to a Track (only set for type = "track")
        /// </summary>
        [JsonPropertyName("trackId")]
        public int? TrackId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isSensitive")]
        public bool IsSensitive { get; set; } = false;

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        /// <summary>
        /// When the event actually occurred. Defaults to CreatedAt, user-editable.
        /// </summary>
        [JsonPropertyName("occurAt")]
        public DateTime? OccurAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        // Navigation property
        public LifeLogTrack? Track { get; set; }
    }
}
