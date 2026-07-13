using System.Text.Json.Serialization;

namespace SuperAppModels.Models.DailyLog
{
    /// <summary>
    /// DailyLog entity — 1 row per (user_id, log_date). Maps to pro.daily_log table.
    /// Field values live in ValuesJson as a JSON blob keyed by "&lt;section&gt;.&lt;field_key&gt;".
    /// </summary>
    public class DailyLog
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("logDate")]
        public DateTime LogDate { get; set; }

        [JsonPropertyName("valuesJson")]
        public string ValuesJson { get; set; } = "{}";

        /// <summary>
        /// Snapshot of the template that was active when this log was last saved.
        /// Rendered as the form structure for this specific log; null on legacy rows.
        /// </summary>
        [JsonPropertyName("templateJson")]
        public string? TemplateJson { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
