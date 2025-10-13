using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class CreateTagRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(7, ErrorMessage = "Color must be in hex format (#RRGGBB)")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format (#RRGGBB)")]
        public string? Color { get; set; }

        public int? ParentId { get; set; }

        [StringLength(255, ErrorMessage = "Slug cannot exceed 255 characters")]
        public string? Slug { get; set; }

        [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
        public string? Icon { get; set; }

        public bool IsPublic { get; set; } = false;

        [StringLength(255, ErrorMessage = "Public slug cannot exceed 255 characters")]
        public string? PublicSlug { get; set; }

        public int UserId { get; set; }
    }
}