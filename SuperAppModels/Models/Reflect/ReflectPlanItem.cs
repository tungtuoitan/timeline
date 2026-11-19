using System.Text.Json.Serialization;

namespace SuperAppModels.Models.Reflect
{
    /// <summary>
    /// One task planned for one day. Maps to pro.reflect_plan_item. The id is made by the client (GUID text).
    /// Anything that is not a column yet (start time, estimate, linked task...) lives in Extra as JSON text.
    /// </summary>
    public class ReflectPlanItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("planDate")]
        public DateTime PlanDate { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        /// <summary>Free text: planned, done, skipped...</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "planned";

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; } = "";

        [JsonPropertyName("extra")]
        public string Extra { get; set; } = "{}";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
