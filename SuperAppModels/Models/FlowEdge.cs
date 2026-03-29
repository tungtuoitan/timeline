using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Arbitrary connection between two nodes (task or project) in the Task Flow view.
    /// Maps to pro.flow_edge table.
    /// </summary>
    [Table("flow_edge", Schema = "pro")]
    public class FlowEdge
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        [Column("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("sourceId")]
        [Column("source_id")]
        public int SourceId { get; set; }

        /// <summary>"task" | "project"</summary>
        [JsonPropertyName("sourceType")]
        [Column("source_type")]
        public string SourceType { get; set; } = "task";

        /// <summary>"top" | "bottom" | "left" | "right"</summary>
        [JsonPropertyName("sourceHandle")]
        [Column("source_handle")]
        public string SourceHandle { get; set; } = "bottom";

        [JsonPropertyName("targetId")]
        [Column("target_id")]
        public int TargetId { get; set; }

        [JsonPropertyName("targetType")]
        [Column("target_type")]
        public string TargetType { get; set; } = "task";

        [JsonPropertyName("targetHandle")]
        [Column("target_handle")]
        public string TargetHandle { get; set; } = "top";

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        /// <summary>"forward" | "backward" | "both"</summary>
        [JsonPropertyName("arrowDirection")]
        [Column("arrow_direction")]
        public string ArrowDirection { get; set; } = "forward";

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
