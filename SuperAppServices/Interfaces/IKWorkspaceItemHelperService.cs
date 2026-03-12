using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    public interface IKWorkspaceItemHelperService
    {
        // VALIDATION
        string? ValidateRequest(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict);

        Task<string?> CheckCircularDependencyAsync(
            int itemId,
            int newParentId,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict);

        // ACTION PROCESSING (Track Changes Only, No SaveChanges!)
        Task ProcessCreateActionAsync(
            KUpsertWorkspaceItemRequest request,
            int userId,
            int workspaceId,
            List<KWorkspaceItemEntity> upsertedItems);

        void ProcessUpdateNodeAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        void ProcessMoveAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        Task ProcessMoveCrossActionAsync(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        Task<List<KWorkspaceItemEntity>> GetAllDescendantsAsync(int parentWorkspaceItemId);

        void ProcessDeleteAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        void ProcessRestoreAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems);

        // PATH SYNC
        Task SyncPathIdsAsync(List<KWorkspaceItemEntity> upsertedItems);

        Task RebuildPathIdsAsync(KWorkspaceItemEntity item);

        Task UpdateDescendantPathIdsAsync(int parentId, string oldPathIds, string newPathIds);
    }
}
