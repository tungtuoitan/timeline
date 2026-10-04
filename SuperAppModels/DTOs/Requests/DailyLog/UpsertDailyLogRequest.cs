using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests.DailyLog
{
    /// <summary>
    /// Request to upsert 1 daily log. Server resolves (user_id, log_date) — no Id needed.
    /// If a log exists for that (user, date), values_json is replaced; otherwise a new row is inserted.
    /// </summary>
    public class UpsertDailyLogRequest
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [Required]
        [JsonPropertyName("logDate")]
        public DateOnly LogDate { get; set; }

        /// <summary>Serialized JSON string, keyed by "&lt;section&gt;.&lt;field_key&gt;" → value.</summary>
        [JsonPropertyName("valuesJson")]
        public string ValuesJson { get; set; } = "{}";

        /// <summary>Snapshot of the active template. Persisted so this log renders with its own structure later.</summary>
        [JsonPropertyName("templateJson")]
        public string? TemplateJson { get; set; }

        /// <summary>Optional soft-delete flag (only valid on an existing log).</summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
