using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for creating or updating a workspace item (upsert operation)
    /// If Id is 0, creates a new workspace item. Otherwise, updates the existing item.
    /// Pattern: 100% follows UpsertNoteRequest structure
    /// </summary>
    public class UpsertWorkspaceItemRequest
    {
        /// <summary>
        /// Workspace item ID (0 for create, >0 for update)
        /// Maps to workspace_items.id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Workspace ID (set from route parameter by controller)
        /// Maps to workspace_items.workspace_id
        /// </summary>
        public int? WorkspaceId { get; set; }

        /// <summary>
        /// User ID (set from JWT claims by controller)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Parent folder ID (null for root level items)
        /// Maps to workspace_items.parent_id
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Item type: 2 = folder, 3 = note, 4 = file
        /// Maps to workspace_items.item_type
        /// </summary>
        [Required(ErrorMessage = "ItemType is required")]
        [Range(2, 4, ErrorMessage = "ItemType must be 2 (folder), 3 (note), or 4 (file)")]
        [JsonPropertyName("itemType")]
        public byte ItemType { get; set; }

        /// <summary>
        /// Reference to folder/note/file ID
        /// Maps to workspace_items.item_id
        /// </summary>
        [Required(ErrorMessage = "ItemId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ItemId must be positive")]
        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }

        /// <summary>
        /// Display name (optional for updates, mainly for validation)
        /// </summary>
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// Copy information JSON metadata (future feature)
        /// Maps to workspace_items.copy_info
        /// </summary>
        [StringLength(1000, ErrorMessage = "CopyInfo cannot exceed 1000 characters")]
        [JsonPropertyName("copyInfo")]
        public string? CopyInfo { get; set; }

        /// <summary>
        /// User email who created/updated the item (set by controller)
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Soft delete timestamp (null = active, DateTime = soft deleted)
        /// Enables soft delete/restore via upsert
        /// Maps to workspace_items.deleted_at
        /// </summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
