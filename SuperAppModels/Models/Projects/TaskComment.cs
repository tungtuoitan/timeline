using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Task Comment entity for task discussions and lessons learned
    /// Maps to pro.task_comment table
    /// </summary>
    public class TaskComment
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("taskId")]
        [Column("task_id")]
        public int TaskId { get; set; }

        [JsonPropertyName("parentCommentId")]
        [Column("parent_comment_id")]
        public int? ParentCommentId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>comment | decision | devlog | track (see TaskCommentTypes)</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = TaskCommentTypes.Comment;

        /// <summary>When it actually happened; null = CreatedAt</summary>
        [JsonPropertyName("occurredAt")]
        [Column("occurred_at")]
        public DateTime? OccurredAt { get; set; }

        [JsonPropertyName("createdAt")]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }

    public static class TaskCommentTypes
    {
        public const string Comment = "comment";
        public const string Decision = "decision";
        public const string Devlog = "devlog";
        public const string Track = "track";

        public static readonly HashSet<string> All = new() { Comment, Decision, Devlog, Track };
    }
}
