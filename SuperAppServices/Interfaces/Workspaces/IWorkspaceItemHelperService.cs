using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Interface for workspace item helper operations with action-based batch processing
    /// </summary>
    public interface IWorkspaceItemHelperService
    {
        // =====================================================================
        // VALIDATION METHODS
        // =====================================================================

        /// <summary>
        /// Validate request based on action type
        /// Returns error message if invalid, null if valid
        /// </summary>
        string? ValidateRequest(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<int> existingFolderIds,
            List<int> existingNoteIds,
            List<int> existingFileIds,
            Dictionary<int, Folder> foldersToUpdateDict,
            Dictionary<int, Note> notesToUpdateDict,
            Dictionary<int, SuperAppModels.Models.File> filesToUpdateDict);

        /// <summary>
        /// Check for circular dependency when moving an item
        /// Returns error message if circular dependency detected, null if safe
        /// </summary>
        Task<string?> CheckCircularDependencyAsync(
            int itemId,
            int newParentId,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict);

        /// <summary>
        /// Check if request has entity data (for CREATE/UPDATE actions)
        /// </summary>
        bool HasEntityData(UpsertWorkspaceItemRequest request, byte? entityType = null);

        // =====================================================================
        // ACTION PROCESSING METHODS (Track Changes Only, No SaveChanges!)
        // =====================================================================

        /// <summary>
        /// CREATE: Create new entity + workspace_item
        /// </summary>
        Task ProcessCreateActionAsync(
            UpsertWorkspaceItemRequest request,
            int userId,
            int workspaceId,
            List<WorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// ADD: Add existing entity to workspace
        /// </summary>
        void ProcessAddAction(
            UpsertWorkspaceItemRequest request,
            int workspaceId,
            List<WorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// MOVE: Change workspace_item location
        /// </summary>
        void ProcessMoveAction(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<WorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// MOVE CROSS: Move workspace_item to another workspace with all descendants
        /// </summary>
        Task ProcessMoveCrossActionAsync(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<WorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// Recursively get all descendants of a workspace item
        /// </summary>
        Task<List<WorkspaceItemEntity>> GetAllDescendantsAsync(int parentWorkspaceItemId);

        /// <summary>
        /// UPDATE FOLDER: Update folder data (FOLDER ONLY)
        /// Notes and Files use their own entity-specific APIs for updates
        /// </summary>
        Task ProcessUpdateFolderActionAsync(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            Dictionary<int, Folder> foldersToUpdateDict);

        /// <summary>
        /// DELETE: Soft delete workspace_item
        /// </summary>
        void ProcessDeleteAction(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<WorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// RESTORE: Restore deleted workspace_item
        /// </summary>
        void ProcessRestoreAction(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<WorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// Sync PathIds and Keywords after Create/Move/Rename operations
        /// Also handles hard deleting keywords for deleted items
        /// </summary>
        Task SyncPathIdsAndKeywordsAsync(
            List<UpsertWorkspaceItemRequest> requests,
            List<WorkspaceItemEntity> upsertedItems,
            int userId);

        /// <summary>
        /// Rebuild PathIds for a workspace item based on its parent
        /// </summary>
        Task RebuildPathIdsAsync(WorkspaceItemEntity item, int userId);

        /// <summary>
        /// Update PathIds for all descendants when parent moves
        /// </summary>
        Task UpdateDescendantPathIdsAsync(int parentId, string oldPathIds, string newPathIds, int userId);
    }
}
