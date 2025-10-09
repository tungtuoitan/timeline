using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class GoogleCodeRequest
    {
        [Required(ErrorMessage = "Authorization code is required")]
        [StringLength(2000, ErrorMessage = "Authorization code cannot exceed 2000 characters")]
        public string Code { get; set; } = string.Empty;
    }
}