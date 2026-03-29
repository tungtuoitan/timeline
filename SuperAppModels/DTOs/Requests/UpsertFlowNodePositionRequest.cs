using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request to save (upsert) a node's canvas position.
    /// Upserts on (user_id, node_id, node_type).
    /// </summary>
    public class UpsertFlowNodePositionRequest
    {
        [Required]
        public int NodeId { get; set; }

        /// <summary>"task" | "project"</summary>
        public string NodeType { get; set; } = "task";

        public double X { get; set; }
        public double Y { get; set; }
    }
}
