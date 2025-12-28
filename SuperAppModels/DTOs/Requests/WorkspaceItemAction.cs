namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Explicit action enum for workspace item batch operations
    /// Eliminates ambiguity in API requests (e.g., move to root with parentId=null vs restore with deletedAt=null)
    /// Follows Microsoft Graph API pattern for batch operations
    /// </summary>
    public enum WorkspaceItemAction
    {
        /// <summary>
        /// CREATE new entity + workspace_item
        /// Required: ItemType, EntityData (FolderData/NoteData/FileData)
        /// Optional: ParentId (null = root level), WorkspaceId
        /// Example: Creating a new folder at root level
        /// </summary>
        Create = 1,

        /// <summary>
        /// ADD existing entity to workspace
        /// Required: ItemType, ItemId
        /// Optional: ParentId (null = root level), WorkspaceId
        /// Example: Adding an existing note to a workspace
        /// </summary>
        Add = 2,

        /// <summary>
        /// MOVE workspace_item to new location
        /// Required: Id + (ParentId OR WorkspaceId)
        /// Optional: Both ParentId and WorkspaceId for cross-workspace move
        /// Example: Moving a folder to a different parent or workspace
        /// </summary>
        Move = 3,

        /// <summary>
        /// UPDATE entity data (folder/note/file properties)
        /// Required: Id, EntityData
        /// Optional: None
        /// Example: Updating a folder's name and color
        /// </summary>
        Update = 4,

        /// <summary>
        /// SOFT DELETE workspace_item
        /// Required: Id
        /// Optional: None
        /// Sets workspace_items.deleted_at to current timestamp
        /// Example: Deleting a note from workspace (recoverable)
        /// </summary>
        Delete = 5,

        /// <summary>
        /// RESTORE deleted workspace_item
        /// Required: Id
        /// Optional: None
        /// Sets workspace_items.deleted_at to null
        /// Example: Restoring a previously deleted file
        /// </summary>
        Restore = 6
    }
}
