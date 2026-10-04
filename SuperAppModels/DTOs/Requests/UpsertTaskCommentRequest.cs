using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for creating or updating a task comment (upsert operation)
    /// If Id is 0, creates a new comment. Otherwise, updates the existing comment.
    /// </summary>
    public class UpsertTaskCommentRequest
    {
        /// <summary>comment | decision | devlog | track. Create: null = "comment". Update: null = unchanged.</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>When it actually happened (null = created time). Update: null = unchanged.</summary>
        [JsonPropertyName("occurredAt")]
        public DateTime? OccurredAt { get; set; }

        /// <summary>
        /// Comment ID (0 for create, >0 for update)
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "TaskId is required")]
        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("parentCommentId")]
        public int? ParentCommentId { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>Set by controller from JWT claims</summary>
        [JsonPropertyName("userId")]
        public int? UserId { get; set; }

        /// <summary>Soft delete timestamp (null = active)</summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
