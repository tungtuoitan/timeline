using System.Runtime.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Explicit action enum for workspace item batch operations
    /// Eliminates ambiguity in API requests (e.g., move to root with parentId=null vs restore with deletedAt=null)
    /// Follows Microsoft Graph API pattern for batch operations
    /// Serializes as string values (CREATE, ADD, MOVE, UPDATE, DELETE, RESTORE)
    /// </summary>
    public enum WorkspaceItemAction
    {
        /// <summary>
        /// CREATE new entity + workspace_item
        /// Required: ItemType, EntityData (FolderData/NoteData/FileData)
        /// Optional: ParentId (null = root level), WorkspaceId
        /// Example: Creating a new folder at root level
        /// </summary>
        [EnumMember(Value = "CREATE")]
        Create,

        /// <summary>
        /// ADD existing entity to workspace
        /// Required: ItemType, ItemId
        /// Optional: ParentId (null = root level), WorkspaceId
        /// Example: Adding an existing note to a workspace
        /// </summary>
        [EnumMember(Value = "ADD")]
        Add,

        /// <summary>
        /// MOVE workspace_item to new location (within same workspace)
        /// Required: Id + ParentId
        /// Optional: None
        /// Example: Moving a folder to a different parent folder
        /// </summary>
        [EnumMember(Value = "MOVE")]
        Move,

        /// <summary>
        /// MOVE CROSS workspace_item to another workspace
        /// Required: Id + WorkspaceId (target workspace)
        /// Optional: ParentId (target parent folder in new workspace, null = root)
        /// Updates workspace_id for item and ALL descendants recursively
        /// Example: Moving a folder with all its contents to another workspace
        /// </summary>
        [EnumMember(Value = "MOVECROSS")]
        MoveCross,

        /// <summary>
        /// UPDATE FOLDER data (name, description, color, icon, etc.)
        /// Required: Id, FolderData
        /// Optional: None
        /// Note: Only for folders. Notes/Files use their own entity-specific APIs.
        /// Example: Updating a folder's name and color
        /// </summary>
        [EnumMember(Value = "UPDATEFOLDER")]
        UpdateFolder,

        /// <summary>
        /// SOFT DELETE workspace_item
        /// Required: Id
        /// Optional: None
        /// Sets workspace_items.deleted_at to current timestamp
        /// Example: Deleting a note from workspace (recoverable)
        /// </summary>
        [EnumMember(Value = "DELETE")]
        Delete,

        /// <summary>
        /// RESTORE deleted workspace_item
        /// Required: Id
        /// Optional: None
        /// Sets workspace_items.deleted_at to null
        /// Example: Restoring a previously deleted file
        /// </summary>
        [EnumMember(Value = "RESTORE")]
        Restore
    }
}
