using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// LifeLog Track entity - represents a recurring behavior/event to track
    /// Maps to log.track table
    /// </summary>
    public class LifeLogTrack : ITimestampEntity
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isSensitive")]
        public bool IsSensitive { get; set; } = false;

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
