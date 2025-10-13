using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class StandardRegistryRequest
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Code must be between 1 and 100 characters")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Type must be between 1 and 50 characters")]
        public string Type { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}