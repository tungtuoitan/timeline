using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for creating or updating a folder
    /// </summary>
    public class UpsertFolderRequest
    {
        /// <summary>
        /// Folder ID to update (optional for create, required for update)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Id must be positive")]
        public int? Id { get; set; }

        /// <summary>
        /// Folder name (required, max 255 characters)
        /// </summary>
        [Required(ErrorMessage = "Folder name is required")]
        [StringLength(255, ErrorMessage = "Folder name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Folder description (optional, max 1000 characters)
        /// </summary>
        //[StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Folder color in hex format (optional, e.g., "#3B82F6")
        /// </summary>
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code (e.g., #3B82F6)")]
        public string? Color { get; set; }

        /// <summary>
        /// Folder icon name or emoji (optional, max 50 characters)
        /// </summary>
        [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
        public string? Icon { get; set; }

        /// <summary>
        /// Parent folder ID (optional, null = root level)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ParentId must be positive")]
        public int? ParentId { get; set; }
    }
}
