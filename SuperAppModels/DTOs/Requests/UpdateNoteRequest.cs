using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class UpdateNoteRequest
    {
        public int NoteId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string? Type { get; set; }

        public List<int>? TagIds { get; set; }
    }
}
