using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// File entity data for batch upsert
    /// Contains all file properties needed for insert/update
    /// </summary>
    public class UpsertFileData
    {
        /// <summary>
        /// File ID (0 for create, >0 for update)
        /// Maps to files.id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID (set from JWT claims by controller)
        /// Maps to files.user_id
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// File name/title
        /// Maps to files.name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// File URL
        /// Maps to files.url
        /// </summary>
        [StringLength(1000, ErrorMessage = "Url cannot exceed 1000 characters")]
        public string? Url { get; set; }

        /// <summary>
        /// File size in bytes
        /// Maps to files.file_size
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// MIME type (e.g., "application/pdf")
        /// Maps to files.mime_type
        /// </summary>
        [StringLength(100, ErrorMessage = "MimeType cannot exceed 100 characters")]
        public string? MimeType { get; set; }

        /// <summary>
        /// File extension (e.g., "pdf", "jpg")
        /// Maps to files.extension
        /// </summary>
        [StringLength(20, ErrorMessage = "Extension cannot exceed 20 characters")]
        public string? Extension { get; set; }

        /// <summary>
        /// Status code
        /// Maps to files.status_code
        /// </summary>
        [StringLength(50, ErrorMessage = "StatusCode cannot exceed 50 characters")]
        public string? StatusCode { get; set; }

        /// <summary>
        /// Soft delete timestamp (null = active, DateTime = soft deleted)
        /// Maps to files.deleted_at
        /// </summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
