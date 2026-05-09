using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class HardDeleteTasksRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one task ID is required")]
        [MaxLength(200, ErrorMessage = "Maximum 200 tasks can be deleted at once")]
        public List<int> Ids { get; set; } = new();
    }
}
