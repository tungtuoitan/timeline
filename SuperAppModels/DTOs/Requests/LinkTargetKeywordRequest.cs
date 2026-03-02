using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class LinkTargetKeywordRequest
    {
        [JsonPropertyName("targetId")]
        public int TargetId { get; set; }

        [JsonPropertyName("targetType")]
        public string TargetType { get; set; } = string.Empty;

        [JsonPropertyName("keywordId")]
        public int KeywordId { get; set; }
    }
}
