using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Note entity data for batch upsert
    /// Contains all note properties needed for insert/update
    /// Mirrors UpsertNoteRequest structure
    /// </summary>
    public class UpsertNoteData
    {
        /// <summary>
        /// Note ID (0 for create, >0 for update)
        /// Maps to notes.id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID (set from JWT claims by controller)
        /// Maps to notes.user_id
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Note name/title
        /// Maps to notes.name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Note description/content preview
        /// Maps to notes.description
        /// </summary>
        //[StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Note status code
        /// Maps to notes.status_code
        /// </summary>
        [StringLength(50, ErrorMessage = "StatusCode cannot exceed 50 characters")]
        public string? StatusCode { get; set; }

        /// <summary>
        /// Tag IDs associated with this note
        /// </summary>
        [JsonPropertyName("tags")]
        public List<int>? TagIds { get; set; }

        /// <summary>
        /// Soft delete timestamp (null = active, DateTime = soft deleted)
        /// Maps to notes.deleted_at
        /// </summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
