using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for creating a new hashtag
    /// </summary>
    public class CreateHashtagRequest
    {
        /// <summary>
        /// Tag name (required, max 100 characters)
        /// Can include # or not (will be auto-removed for storage)
        /// </summary>
        [Required(ErrorMessage = "Tag name is required")]
        [StringLength(100, ErrorMessage = "Tag name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tag color in hex format (optional, default: #6B7280 gray)
        /// </summary>
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code (e.g., #6B7280)")]
        public string? Color { get; set; }
    }
}
