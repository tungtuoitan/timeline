using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class CreateNoteRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
        public string? Tags { get; set; }

        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string? Type { get; set; }

        public string? CreatedBy { get; set; }
    }
}
