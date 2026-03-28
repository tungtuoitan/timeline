using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Folder entity data for batch upsert
    /// Contains all folder properties needed for insert/update
    /// </summary>
    public class UpsertFolderData
    {
        /// <summary>
        /// Folder ID (0 for create, >0 for update)
        /// Maps to folders.id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID (set from JWT claims by controller)
        /// Maps to folders.user_id
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Folder name
        /// Maps to folders.name
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Folder description
        /// Maps to folders.description
        /// </summary>
        //[StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }


        /// <summary>
        /// Folder color (hex code)
        /// Maps to folders.color
        /// </summary>
        [StringLength(20, ErrorMessage = "Color cannot exceed 20 characters")]
        public string? Color { get; set; }

        /// <summary>
        /// Folder icon (emoji or icon name)
        /// Maps to folders.icon
        /// </summary>
        [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
        public string? Icon { get; set; }

        /// <summary>
        /// Node type: "entity" | "question" | null
        /// Maps to k.node.node_type
        /// </summary>
        [StringLength(50, ErrorMessage = "NodeType cannot exceed 50 characters")]
        [JsonPropertyName("nodeType")]
        public string? NodeType { get; set; }

        /// <summary>
        /// Workflow status — "draft" | null (active).
        /// Pass null to activate a draft node.
        /// Maps to k.node.status_code
        /// </summary>
        [StringLength(50, ErrorMessage = "StatusCode cannot exceed 50 characters")]
        [JsonPropertyName("statusCode")]
        public string? StatusCode { get; set; }

        /// <summary>
        /// Soft delete timestamp (null = active, DateTime = soft deleted)
        /// Maps to folders.deleted_at
        /// </summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
