using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for creating a new note
    /// </summary>
    public class CreateNoteRequest
    {
        /// <summary>
        /// Note name/title
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Note description/content
        /// </summary>
        [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Comma-separated tags for the note
        /// </summary>
        [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
        public string? Tags { get; set; }

        /// <summary>
        /// Note type/category
        /// </summary>
        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string? Type { get; set; }

        // Note: CreatedBy will be set from authenticated user context, not from request
    }
}
