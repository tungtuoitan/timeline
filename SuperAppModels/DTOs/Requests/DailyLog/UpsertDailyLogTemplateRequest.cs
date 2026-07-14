using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests.DailyLog
{
    /// <summary>
    /// Request to upsert 1 field template entry (batch endpoint accepts a list).
    /// If Id is 0, a new field is created; otherwise the existing row is updated.
    /// Set DeletedAt to soft-delete a field.
    /// </summary>
    public class UpsertDailyLogTemplateRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [Required, StringLength(16)]
        [JsonPropertyName("section")]
        public string Section { get; set; } = "input";

        [Required, StringLength(64)]
        [JsonPropertyName("fieldKey")]
        public string FieldKey { get; set; } = string.Empty;

        [Required, StringLength(128)]
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [Required, StringLength(16)]
        [JsonPropertyName("fieldType")]
        public string FieldType { get; set; } = "text";

        [JsonPropertyName("rangeMin")]
        public double? RangeMin { get; set; }

        [JsonPropertyName("rangeMax")]
        public double? RangeMax { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("groupOrder")]
        public int? GroupOrder { get; set; }

        [JsonPropertyName("groupLabel")]
        public string? GroupLabel { get; set; }

        [JsonPropertyName("lineOrder")]
        public int? LineOrder { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
