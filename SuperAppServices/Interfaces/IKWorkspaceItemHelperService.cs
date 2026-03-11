using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Interface for workspace item helper operations with action-based batch processing
    /// </summary>
    public interface IKWorkspaceItemHelperService
    {
        // =====================================================================
        // VALIDATION METHODS
        // =====================================================================

        /// <summary>
        /// Validate request based on action type 
        /// Returns error message if invalid, null if valid
        /// </summary>
        string? ValidateRequest(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
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
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict);

        /// <summary>
        /// Check if request has entity data (for CREATE/UPDATE actions)
        /// </summary>
        bool HasEntityData(KUpsertWorkspaceItemRequest request, byte? entityType = null);

        // =====================================================================
        // ACTION PROCESSING METHODS (Track Changes Only, No SaveChanges!)
        // =====================================================================

        /// <summary>
        /// CREATE: Create new entity + workspace_item
        /// </summary>
        Task ProcessCreateActionAsync(
            KUpsertWorkspaceItemRequest request,
            int userId,
            int workspaceId,
            List<KWorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// ADD: Add existing entity to workspace
        /// </summary>
        void ProcessAddAction(
            KUpsertWorkspaceItemRequest request,
            int workspaceId,
            List<KWorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// MOVE: Change workspace_item location
        /// </summary>
        void ProcessMoveAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// MOVE CROSS: Move workspace_item to another workspace with all descendants
        /// </summary>
        Task ProcessMoveCrossActionAsync(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// Recursively get all descendants of a workspace item
        /// </summary>
        Task<List<KWorkspaceItemEntity>> GetAllDescendantsAsync(int parentWorkspaceItemId);

        /// <summary>
        /// UPDATE FOLDER: Update folder data (FOLDER ONLY)
        /// Notes and Files use their own entity-specific APIs for updates
        /// </summary>
        Task ProcessUpdateFolderActionAsync(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            Dictionary<int, Folder> foldersToUpdateDict);

        /// <summary>
        /// DELETE: Soft delete workspace_item
        /// </summary>
        void ProcessDeleteAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// RESTORE: Restore deleted workspace_item
        /// </summary>
        void ProcessRestoreAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        /// <summary>
        /// Sync PathIds and Keywords after Create/Move/Rename operations
        /// Also handles hard deleting keywords for deleted items
        /// </summary>
        Task SyncPathIdsAndKeywordsAsync(
            List<KUpsertWorkspaceItemRequest> requests,
            List<KWorkspaceItemEntity> upsertedItems,
            int userId);

        /// <summary>
        /// Rebuild PathIds for a workspace item based on its parent
        /// </summary>
        Task RebuildPathIdsAsync(KWorkspaceItemEntity item, int userId);

        /// <summary>
        /// Update PathIds for all descendants when parent moves
        /// </summary>
        Task UpdateDescendantPathIdsAsync(int parentId, string oldPathIds, string newPathIds, int userId);
    }
}
