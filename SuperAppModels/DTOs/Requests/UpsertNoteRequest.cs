using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for creating or updating a note (upsert operation)
    /// If Id is 0, creates a new note. Otherwise, updates the existing note.
    /// </summary>
    public class UpsertNoteRequest
    {
        /// <summary>
        /// Note ID (0 for create, >0 for update)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID (set from JWT claims by controller)
        /// </summary>
        public int? UserId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        //[StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string? Type { get; set; }

        [StringLength(50, ErrorMessage = "StatusCode cannot exceed 50 characters")]
        public string? StatusCode { get; set; }

        [JsonPropertyName("tags")]
        public List<int>? TagIds { get; set; }

        /// <summary>
        /// User email who created/updated the note
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Optional: Soft delete timestamp (null = active, DateTime = soft deleted)
        /// Enables soft delete/restore via upsert
        /// </summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
