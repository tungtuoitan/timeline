namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service for syncing keywords when workspace/folder/note operations occur
    /// Handles cascade updates for all children
    /// </summary>
    public interface IKeywordSyncService
    {
        /// <summary>
        /// Sync workspace keyword and cascade update all children
        /// Call when: Create/Rename workspace
        /// </summary>
        Task SyncWorkspaceAsync(int workspaceId, int userId);

        /// <summary>
        /// Sync folder keyword and cascade update all children
        /// Call when: Create/Rename/Move folder
        /// </summary>
        Task SyncFolderAsync(int folderWorkspaceItemId, int workspaceId, int userId);

        /// <summary>
        /// Sync note keyword and cascade update all headings
        /// Call when: Create/Rename note, Update description (for headings)
        /// </summary>
        Task SyncNoteAsync(int noteWorkspaceItemId, int workspaceId, int userId);

        /// <summary>
        /// Move folder to new workspace and cascade update all children
        /// Call when: Move folder to different workspace
        /// </summary>
        Task MoveFolderAsync(int folderWorkspaceItemId, int newWorkspaceId, int? newParentFolderId, int userId);

        /// <summary>
        /// Move note to new location and cascade update all headings
        /// Call when: Move note to different workspace/folder
        /// </summary>
        Task MoveNoteAsync(int noteWorkspaceItemId, int newWorkspaceId, int? newParentFolderId, int userId);

        /// <summary>
        /// Delete keywords when workspace is deleted
        /// </summary>
        Task DeleteWorkspaceKeywordsAsync(int workspaceId);

        /// <summary>
        /// Delete keywords when folder is deleted
        /// </summary>
        Task DeleteFolderKeywordsAsync(int folderWorkspaceItemId);

        /// <summary>
        /// Delete keywords when note is deleted
        /// </summary>
        Task DeleteNoteKeywordsAsync(int noteWorkspaceItemId);

        /// <summary>
        /// Rebuild all keywords for a user (background job)
        /// </summary>
        Task RebuildAllKeywordsAsync(int userId);
    }
}
