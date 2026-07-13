using System.Text.Json.Serialization;

namespace SuperAppModels.Models.DailyLog
{
    /// <summary>
    /// Field template row — defines a field inside a section of the user's daily log form.
    /// Per-user global (applies to all logs of this user). Maps to pro.daily_log_field_template.
    /// </summary>
    public class DailyLogFieldTemplate
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        /// <summary>"input" | "output"</summary>
        [JsonPropertyName("section")]
        public string Section { get; set; } = string.Empty;

        /// <summary>Stable key used inside daily_log.values_json ("general", "note", "emotion_note", ...)</summary>
        [JsonPropertyName("fieldKey")]
        public string FieldKey { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        /// <summary>"text" | "longText" | "checkbox" | "number" | "range"</summary>
        [JsonPropertyName("fieldType")]
        public string FieldType { get; set; } = "text";

        /// <summary>Min bound for fieldType = "range". Null for other types.</summary>
        [JsonPropertyName("rangeMin")]
        public double? RangeMin { get; set; }

        /// <summary>Max bound for fieldType = "range". Null for other types.</summary>
        [JsonPropertyName("rangeMax")]
        public double? RangeMax { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
