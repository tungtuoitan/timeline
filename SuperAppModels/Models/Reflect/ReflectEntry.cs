using System.Text.Json.Serialization;

namespace SuperAppModels.Models.Reflect
{
    /// <summary>
    /// One row of the Reflect journal (type / what I think / what really happened). Maps to pro.reflect_entry.
    /// The id is made by the client (GUID text) so a row can be saved again and again without a round trip.
    /// </summary>
    public class ReflectEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        /// <summary>Free label, e.g. "Plan", "Prediction"</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        /// <summary>Written before doing the work</summary>
        [JsonPropertyName("think")]
        public string Think { get; set; } = "";

        /// <summary>Written afterwards</summary>
        [JsonPropertyName("real")]
        public string Real { get; set; } = "";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
