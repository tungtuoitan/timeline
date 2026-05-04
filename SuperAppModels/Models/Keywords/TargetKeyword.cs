using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Links any target entity (TASK, NOTE, PROJECT, etc.) to a keyword
    /// Maps to pro.TargetKeywords table
    /// </summary>
    public class TargetKeyword
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("targetId")]
        public int TargetId { get; set; }

        /// <summary>
        /// Target entity type: "TASK", "NOTE", "PROJECT", "WORKSPACE", "FOLDER"
        /// </summary>
        [JsonPropertyName("targetType")]
        public string TargetType { get; set; } = string.Empty;

        [JsonPropertyName("keywordId")]
        public int KeywordId { get; set; }
    }
}
