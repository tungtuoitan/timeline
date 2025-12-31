using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for creating or updating a workspace
    /// </summary>
    public class UpsertWorkspaceRequest
    {
        /// <summary>
        /// Workspace ID (null for create, value for update)
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Workspace name (required, max 255 chars)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Workspace description (optional, max 1000 chars)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Status code for workspace (optional, max 50 chars)
        /// </summary>
        public string? StatusCode { get; set; }

        /// <summary>
        /// Tag IDs associated with this workspace
        /// </summary>
        [JsonPropertyName("tags")]
        public List<int>? TagIds { get; set; }

        /// <summary>
        /// User ID (set by backend from authenticated user)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Optional: Soft delete timestamp (null = active, DateTime = soft deleted)
        /// Enables soft delete/restore via upsert
        /// </summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
