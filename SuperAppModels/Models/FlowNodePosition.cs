using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Saved node position for a user in the Task Flow view.
    /// Maps to pro.flow_node_position table.
    /// Unique per (user_id, node_id, node_type).
    /// </summary>
    [Table("flow_node_position", Schema = "pro")]
    public class FlowNodePosition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        [Column("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("nodeId")]
        [Column("node_id")]
        public int NodeId { get; set; }

        /// <summary>"task" | "project"</summary>
        [JsonPropertyName("nodeType")]
        [Column("node_type")]
        public string NodeType { get; set; } = "task";

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("createdAt")]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
