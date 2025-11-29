using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for tagging an entity with a hashtag
    /// </summary>
    public class TagEntityRequest
    {
        /// <summary>
        /// Tag ID to apply
        /// </summary>
        [Required(ErrorMessage = "Tag ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Tag ID must be a positive integer")]
        public int TagId { get; set; }

        /// <summary>
        /// Entity type (workspace, folder, note, file)
        /// </summary>
        [Required(ErrorMessage = "Entity type is required")]
        [RegularExpression(@"^(workspace|folder|note|file)$", ErrorMessage = "Entity type must be workspace, folder, note, or file")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Entity ID to tag
        /// </summary>
        [Required(ErrorMessage = "Entity ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Entity ID must be a positive integer")]
        public int EntityId { get; set; }
    }
}
