using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for upserting external keywords
    /// Pattern: Similar to UpsertWorkspaceItemRequest
    /// </summary>
    public class UpsertExternalKeywordRequest
    {
        /// <summary>
        /// Keyword ID (null for insert, value for update)
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Display name of the keyword
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL link (unique identifier)
        /// </summary>
        [Required(ErrorMessage = "Link is required")]
        [StringLength(2000, ErrorMessage = "Link cannot exceed 2000 characters")]
        public string Link { get; set; } = string.Empty;

        /// <summary>
        /// Optional description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// User ID (set by controller from JWT token)
        /// </summary>
        public int UserId { get; set; }
    }
}
