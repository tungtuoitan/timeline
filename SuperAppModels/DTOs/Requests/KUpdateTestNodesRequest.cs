using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Batch update questions for a test:
    ///   - AddQuestions: new questions to create in k.question
    ///   - ToggleQuestionIds: k.question.id list to flip is_active
    ///   - DeleteQuestionIds: k.question.id list to permanently delete
    /// </summary>
    public class KUpdateQuestionsRequest
    {
        /// <summary>New questions to add to this test</summary>
        [JsonPropertyName("addQuestions")]
        public List<KNewQuestionItem> AddQuestions { get; set; } = [];

        /// <summary>k.question IDs whose is_active to toggle</summary>
        [JsonPropertyName("toggleQuestionIds")]
        public List<int> ToggleQuestionIds { get; set; } = [];

        /// <summary>k.question IDs to soft-delete (set deleted_at)</summary>
        [JsonPropertyName("deleteQuestionIds")]
        public List<int> DeleteQuestionIds { get; set; } = [];

        /// <summary>k.question IDs to restore (clear deleted_at)</summary>
        [JsonPropertyName("restoreQuestionIds")]
        public List<int> RestoreQuestionIds { get; set; } = [];

        /// <summary>Existing questions to update name/description</summary>
        [JsonPropertyName("updateQuestions")]
        public List<KUpdateQuestionItem> UpdateQuestions { get; set; } = [];
    }

    public class KUpdateQuestionItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class KNewQuestionItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
