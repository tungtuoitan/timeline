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
    ///    Required: Action=Create, ItemType, EntityData
    ///    Optional: ParentId (null = root), WorkspaceId
    ///    Example: { "action": "create", "itemType": 2, "parentId": null, "folderData": {...} }
    ///
    /// 2. ADD (existing entity to workspace):
    ///    Required: Action=Add, ItemType, ItemId
    ///    Optional: ParentId (null = root), WorkspaceId
    ///    Example: { "action": "add", "itemType": 3, "itemId": 456, "parentId": 123 }
    ///
    /// 3. MOVE (change location):
    ///    Required: Action=Move, Id + (ParentId OR WorkspaceId)
    ///    Optional: Both for cross-workspace move
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
    public class UpsertWorkspaceItemRequest
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
        public WorkspaceItemAction Action { get; set; }

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
        /// Parent folder ID
        /// Required for: Move (when moving to different parent)
        /// Optional for: Create, Add (null = root level)
        /// Maps to workspace_items.parent_id
        /// </summary>
        [JsonPropertyName("parentId")]
        public int? ParentId { get; set; }

        /// <summary>
        /// Item type: 2 = folder, 3 = note, 4 = file
        /// Required for: Create, Add
        /// Not used for: Move, Update, Delete, Restore (inferred from existing workspace_item)
        /// Maps to workspace_items.item_type
        /// </summary>
        [Range(2, 4, ErrorMessage = "ItemType must be 2 (folder), 3 (note), or 4 (file)")]
        [JsonPropertyName("itemType")]
        public byte? ItemType { get; set; }

        /// <summary>
        /// Entity ID (references existing folder/note/file)
        /// Required for: Add (itemId > 0)
        /// Not used for: Create, Move, Update, Delete, Restore
        /// Maps to the ID of the entity being referenced
        /// </summary>
        [JsonPropertyName("itemId")]
        public int? ItemId { get; set; }

        /// <summary>
        /// Copy information JSON metadata (future feature)
        /// Optional for: Create, Add
        /// Maps to workspace_items.copy_info
        /// </summary>
        [StringLength(1000, ErrorMessage = "CopyInfo cannot exceed 1000 characters")]
        [JsonPropertyName("copyInfo")]
        public string? CopyInfo { get; set; }

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
        /// Required for: Create (ItemType=2), Update (ItemType=2)
        /// Not used for: Add, Move, Delete, Restore
        /// Contains all folder properties for insert/update
        /// </summary>
        [JsonPropertyName("folderData")]
        public UpsertFolderData? FolderData { get; set; }

        /// <summary>
        /// Note entity data
        /// Required for: Create (ItemType=3), Update (ItemType=3)
        /// Not used for: Add, Move, Delete, Restore
        /// Contains all note properties for insert/update
        /// </summary>
        [JsonPropertyName("noteData")]
        public UpsertNoteData? NoteData { get; set; }

        /// <summary>
        /// File entity data
        /// Required for: Create (ItemType=4), Update (ItemType=4)
        /// Not used for: Add, Move, Delete, Restore
        /// Contains all file properties for insert/update
        /// </summary>
        [JsonPropertyName("fileData")]
        public UpsertFileData? FileData { get; set; }
    }
}
