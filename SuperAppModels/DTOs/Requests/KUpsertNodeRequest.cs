using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Action-based request for k.node batch operations.
    ///
    /// ACTIONS:
    /// 1. CREATE:    { "action": "create",    "nodeData": { "name": "My Node" }, "parentId": null }
    /// 2. UPDATE:    { "action": "update",    "id": 123, "nodeData": { "name": "New Name" } }
    /// 3. MOVE:      { "action": "move",      "id": 456, "parentId": 789 }
    /// 4. MOVECROSS: { "action": "movecross", "id": 456, "knowledgeId": 2, "parentId": null }
    /// 5. DELETE:    { "action": "delete",    "id": 789 }
    /// 6. RESTORE:   { "action": "restore",   "id": 789 }
    /// </summary>
    public class KUpsertNodeRequest
    {
        [Required(ErrorMessage = "Action is required")]
        [JsonPropertyName("action")]
        public KNodeAction Action { get; set; }

        /// <summary>Node ID — required for Update/Move/MoveCross/Delete/Restore</summary>
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        /// <summary>Knowledge ID (set from route; overridable for MoveCross)</summary>
        [JsonPropertyName("knowledgeId")]
        public int? KnowledgeId { get; set; }

        /// <summary>Set from JWT by controller</summary>
        public int? UserId { get; set; }

        /// <summary>Set from JWT by controller</summary>
        public string? CreatedBy { get; set; }

        /// <summary>Parent node ID — null = root</summary>
        [JsonPropertyName("parentId")]
        public int? ParentId { get; set; }

        /// <summary>Node data for Create/Update actions</summary>
        [JsonPropertyName("nodeData")]
        public UpsertFolderData? NodeData { get; set; }
    }
}
