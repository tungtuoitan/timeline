using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Action-based request for workspace item batch operations
    /// Eliminates ambiguity by using explicit Action enum (follows Microsoft Graph API pattern)
    ///
    /// VALIDATION RULES PER ACTION:
    ///
    /// 1. CREATE (new entity + workspace_item):
    ///    Required: Action=Create, EntityType, EntityData
    ///    Optional: ParentId (null = root), WorkspaceId
    ///    Example: { "action": "create", "entityType": 2, "parentId": null, "folderData": {...} }
    ///
    /// 2. ADD (existing entity to workspace):
    ///    Required: Action=Add, EntityType, EntityId 
    ///    Optional: ParentId (null = root), WorkspaceId
    ///    Example: { "action": "add", "entityType": 3, "entityId": 456, "parentId": 123 }
    ///
    /// 3. MOVE (change location):
    ///    Required: Action=Move, Id + (ParentId OR WorkspaceId)
    ///    Optional: Both for cross-workspace move
    ///    ParentId = workspace_items.id of new parent (NOT entity ID!)
    ///    Example: { "action": "move", "id": 789, "parentId": null } ← move to root
    ///
    /// 4. UPDATE (entity properties):
    ///    Required: Action=Update, Id, EntityData
    ///    Optional: None
    ///    Example: { "action": "update", "id": 789, "folderData": { "name": "New Name" } }
    ///
    /// 5. DELETE (soft delete):
    ///    Required: Action=Delete, Id
    ///    Optional: None
    ///    Example: { "action": "delete", "id": 789 }
    ///
    /// 6. RESTORE (un-delete):
    ///    Required: Action=Restore, Id
    ///    Optional: None
    ///    Example: { "action": "restore", "id": 789 }
    /// </summary>
    public class KUpsertWorkspaceItemRequest
    {
        // ============================================================
        // ACTION (REQUIRED FOR ALL OPERATIONS)
        // ============================================================

        /// <summary>
        /// Explicit action to perform on workspace item
        /// Makes API intent crystal clear and eliminates ambiguity
        /// </summary>
        [Required(ErrorMessage = "Action is required")]
        [JsonPropertyName("action")]
        public KWorkspaceItemAction Action { get; set; }

        // ============================================================
        // WORKSPACE_ITEMS TABLE PROPERTIES
        // ============================================================

        /// <summary>
        /// Workspace item ID
        /// Required for: Move, Update, Delete, Restore (Id > 0)
        /// Not used for: Create, Add (Id = 0 or omitted)
        /// Maps to workspace_items.id
        /// </summary>
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        /// <summary>
        /// Workspace ID (set from route parameter by controller, can be overridden for Move action)
        /// Required for: Move (when moving to different workspace)
        /// Optional for: Create, Add
        /// Maps to workspace_items.workspace_id
        /// </summary>
        [JsonPropertyName("workspaceId")]
        public int? WorkspaceId { get; set; }

        /// <summary>
        /// User ID (set from JWT claims by controller)
        /// Automatically populated by controller, not sent by client
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Parent workspace_item ID (SELF-REFERENCING)
        /// Required for: Move (when moving to different parent)
        /// Optional for: Create, Add (null = root level)
        /// Maps to workspace_items.parent_id → references workspace_items.id (NOT entity ID!)
        /// </summary>
        [JsonPropertyName("parentId")]
        public int? ParentId { get; set; }

        /// <summary>
        /// Entity type: 2 = folder, 3 = note, 4 = file
        /// Required for: Create, Add
        /// Not used for: Move, Update, Delete, Restore (inferred from existing workspace_item)
        /// Maps to workspace_items.entity_type
        /// </summary>
        [Range(2, 4, ErrorMessage = "EntityType must be 2 (folder), 3 (note), or 4 (file)")]
        [JsonPropertyName("entityType")]
        public byte? EntityType { get; set; }

        /// <summary>
        /// Entity ID (references existing folder/note/file from entity tables)
        /// Required for: Add (entityId > 0)
        /// Not used for: Create, Move, Update, Delete, Restore
        /// Maps to workspace_items.entity_id → references folders.id | notes.id | files.id
        /// </summary>
        [JsonPropertyName("entityId")]
        public int? EntityId { get; set; }


        /// <summary>
        /// User email who created/updated the item (set by controller)
        /// Automatically populated by controller, not sent by client
        /// </summary>
        public string? CreatedBy { get; set; }

        // ============================================================
        // ENTITY DATA (POLYMORPHIC - SET BASED ON ItemType)
        // ============================================================

        /// <summary>
        /// Folder entity data
        /// Required for: Create (EntityType=2), Update (EntityType=2)
        /// Not used for: Add, Move, Delete, Restore
        /// Contains all folder properties for insert/update
        /// </summary>
        [JsonPropertyName("folderData")]
        public UpsertFolderData? FolderData { get; set; }

        /// <summary>
        /// Note entity data
        /// Required for: Create (EntityType=3), Update (EntityType=3)
        /// Not used for: Add, Move, Delete, Restore
        /// Contains all note properties for insert/update
        /// </summary>
        [JsonPropertyName("noteData")]
        public UpsertNoteData? NoteData { get; set; }

        /// <summary>
        /// File entity data
        /// Required for: Create (EntityType=4), Update (EntityType=4)
        /// Not used for: Add, Move, Delete, Restore
        /// Contains all file properties for insert/update
        /// </summary>
        [JsonPropertyName("fileData")]
        public UpsertFileData? FileData { get; set; }
    }
}
