using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class KMoveNodesRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one node ID is required")]
        public List<int> NodeIds { get; set; } = new();

        /// <summary>Target parent node ID — null = move to root</summary>
        public int? TargetParentId { get; set; }

        /// <summary>Target knowledge ID — null = same knowledge (same-workspace move)</summary>
        public int? TargetKnowledgeId { get; set; }
    }
}
