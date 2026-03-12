using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Action-based request for workspace item batch operations
    /// Each workspace_item is a self-contained node (name, description, color, icon).
    ///
    /// ACTIONS:
    /// 1. CREATE: { "action": "create", "nodeData": { "name": "My Node" }, "parentId": null }
    /// 2. UPDATE: { "action": "update", "id": 123, "nodeData": { "name": "New Name" } }
    /// 3. MOVE:   { "action": "move", "id": 456, "parentId": 789 }
    /// 4. MOVECROSS: { "action": "movecross", "id": 456, "workspaceId": 2, "parentId": null }
    /// 5. DELETE: { "action": "delete", "id": 789 }
    /// 6. RESTORE:{ "action": "restore", "id": 789 }
    /// </summary>
    public class KUpsertWorkspaceItemRequest
    {
        [Required(ErrorMessage = "Action is required")]
        [JsonPropertyName("action")]
        public KWorkspaceItemAction Action { get; set; }

        /// <summary>Workspace item ID — required for Update/Move/MoveCross/Delete/Restore</summary>
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        /// <summary>Workspace ID (set from route by controller; can be overridden for MoveCross)</summary>
        [JsonPropertyName("workspaceId")]
        public int? WorkspaceId { get; set; }

        /// <summary>User ID (set from JWT by controller)</summary>
        public int? UserId { get; set; }

        /// <summary>User email (set by controller)</summary>
        public string? CreatedBy { get; set; }

        /// <summary>Parent workspace_item ID — null = root level</summary>
        [JsonPropertyName("parentId")]
        public int? ParentId { get; set; }

        /// <summary>Node data for Create/Update actions</summary>
        [JsonPropertyName("nodeData")]
        public UpsertFolderData? NodeData { get; set; }
    }
}
