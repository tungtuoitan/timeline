using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for updating an existing note
    /// </summary>
    public class UpdateNoteRequest
    {
        /// <summary>
        /// Note identifier (provided via route, not body)
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Note ID")]
        public int NoteId { get; set; }

        /// <summary>
        /// Updated note name/title
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Updated note description/content
        /// </summary>
        [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Updated comma-separated tags for the note
        /// </summary>
        [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
        public string? Tags { get; set; }

        /// <summary>
        /// Updated note type/category
        /// </summary>
        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string? Type { get; set; }
    }
}
