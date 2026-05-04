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
}
