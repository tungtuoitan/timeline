using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Batch update questions for a knowledge:
    ///   - AddQuestions: new questions to create in k.question
    ///   - ToggleQuestionIds: k.question.id list to flip is_active
    ///   - DeleteQuestionIds: k.question.id list to soft-delete (set deleted_at)
    ///   - RestoreQuestionIds: k.question.id list to restore (clear deleted_at)
    ///   - UpdateQuestions: existing questions to update name/description
    ///   - ResetSrsQuestionIds: k.question.id list to reset SRS state
    /// </summary>
    public class KUpdateQuestionsRequest
    {
        /// <summary>New questions to add to this knowledge</summary>
        [JsonPropertyName("addQuestions")]
        public List<KNewQuestionItem> AddQuestions { get; set; } = [];

        /// <summary>k.question IDs to soft-delete (set deleted_at)</summary>
        [JsonPropertyName("deleteQuestionIds")]
        public List<int> DeleteQuestionIds { get; set; } = [];

        /// <summary>k.question IDs to restore (clear deleted_at)</summary>
        [JsonPropertyName("restoreQuestionIds")]
        public List<int> RestoreQuestionIds { get; set; } = [];

        /// <summary>Existing questions to update name/description</summary>
        [JsonPropertyName("updateQuestions")]
        public List<KUpdateQuestionItem> UpdateQuestions { get; set; } = [];

        /// <summary>k.question IDs to reset SRS state (interval=0, ease=2.5, rep=0, nextReview=null)</summary>
        [JsonPropertyName("resetSrsQuestionIds")]
        public List<int> ResetSrsQuestionIds { get; set; } = [];

        /// <summary>k.question IDs to toggle is_draft flag</summary>
        [JsonPropertyName("toggleDraftQuestionIds")]
        public List<int> ToggleDraftQuestionIds { get; set; } = [];
    }

    public class KUpdateQuestionItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }
    }

    public class KNewQuestionItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }
    }

    /// <summary>Move a question to a different node (nodeId = null → orphan).</summary>
    public class KMoveQuestionRequest
    {
        [JsonPropertyName("nodeId")]
        public int? NodeId { get; set; }
    }
}
