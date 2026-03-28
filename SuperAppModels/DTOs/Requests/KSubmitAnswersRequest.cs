using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class KSubmitAnswersRequest
    {
        [Required]
        public List<KAnswerItem> Answers { get; set; } = [];
    }

    public class KAnswerItem
    {
        [Required]
        [JsonPropertyName("nodeId")]
        public int NodeId { get; set; }

        [JsonPropertyName("answerText")]
        public string? AnswerText { get; set; }
    }
}
