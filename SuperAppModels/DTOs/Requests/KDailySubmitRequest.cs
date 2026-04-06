using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class KDailySubmitRequest
    {
        [Required]
        public List<KDailyAnswerItem> Answers { get; set; } = [];
    }

    public class KDailyAnswerItem
    {
        [Required]
        [JsonPropertyName("questionId")]
        public int QuestionId { get; set; }

        [JsonPropertyName("answerText")]
        public string? AnswerText { get; set; }

        /// <summary>Time in milliseconds the user took to answer.</summary>
        [JsonPropertyName("responseTimeMs")]
        public int? ResponseTimeMs { get; set; }
    }
}
