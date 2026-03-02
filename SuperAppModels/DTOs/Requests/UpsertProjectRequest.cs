using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for creating or updating a project (upsert operation)
    /// If Id is 0, creates a new project. Otherwise, updates the existing project.
    /// </summary>
    public class UpsertProjectRequest
    {
        /// <summary>
        /// Project ID (0 for create, >0 for update)
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// User ID (set by server from JWT token)
        /// </summary>
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = "open";

        /// <summary>
        /// Project start date
        /// </summary>
        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Project end date
        /// </summary>
        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Optional: Soft delete timestamp (null = active, DateTime = soft deleted)
        /// Enables soft delete/restore via upsert
        /// </summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Optional: Workspace ID linked to this project (1:1 relationship)
        /// Set automatically by backend when creating a new project
        /// </summary>
        [JsonPropertyName("workspaceId")]
        public int? WorkspaceId { get; set; }
    }
}
