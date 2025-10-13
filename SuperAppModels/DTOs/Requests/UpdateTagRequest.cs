using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class UpdateTagRequest
    {
        public int TagId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(7, ErrorMessage = "Color must be in hex format (#RRGGBB)")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format (#RRGGBB)")]
        public string? Color { get; set; }
    }
}