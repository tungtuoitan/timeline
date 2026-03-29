using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request to create or update a flow edge (arbitrary connection with optional note).
    /// Pass DeletedAt to soft-delete.
    /// </summary>
    public class UpsertFlowEdgeRequest
    {
        /// <summary>Null = create new; positive int = update existing.</summary>
        public int? Id { get; set; }

        [Required]
        public int SourceId { get; set; }

        /// <summary>"task" | "project"</summary>
        public string SourceType { get; set; } = "task";

        /// <summary>"top" | "bottom" | "left" | "right"</summary>
        public string SourceHandle { get; set; } = "bottom";

        [Required]
        public int TargetId { get; set; }

        /// <summary>"task" | "project"</summary>
        public string TargetType { get; set; } = "task";

        /// <summary>"top" | "bottom" | "left" | "right"</summary>
        public string TargetHandle { get; set; } = "top";

        [MaxLength(500)]
        public string? Note { get; set; }

        /// <summary>"forward" | "backward" | "both"</summary>
        public string ArrowDirection { get; set; } = "forward";

        /// <summary>ISO string to soft-delete; null to restore.</summary>
        public string? DeletedAt { get; set; }
    }
}
