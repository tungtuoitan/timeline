using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class KDeleteNodesRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one node ID is required")]
        [MaxLength(500, ErrorMessage = "Cannot delete more than 500 nodes at once")]
        public List<int> NodeIds { get; set; } = new();
    }
}
