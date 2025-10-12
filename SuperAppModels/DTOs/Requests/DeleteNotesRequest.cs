using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for bulk deleting multiple notes
    /// </summary>
    public class DeleteNotesRequest
    {
        /// <summary>
        /// List of note IDs to delete
        /// </summary>
        [Required(ErrorMessage = "Note IDs are required")]
        [MinLength(1, ErrorMessage = "At least one note ID must be provided")]
        public List<int> NoteIds { get; set; } = new List<int>();
    }
}
