using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class CreateNoteRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
        public string? Tags { get; set; }

        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string? Type { get; set; }

        public string? CreatedBy { get; set; }
    }
}
