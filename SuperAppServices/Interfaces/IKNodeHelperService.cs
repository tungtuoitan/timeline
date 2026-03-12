using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    public interface IKNodeHelperService
    {
        // VALIDATION
        string? ValidateRequest(KUpsertNodeRequest request, Dictionary<int, KNodeEntity> existingNodes);
        Task<string?> CheckCircularDependencyAsync(int nodeId, int newParentId, Dictionary<int, KNodeEntity> existingNodes);

        // ACTION PROCESSING (track changes only — SaveChanges called by KNodeService)
        Task ProcessCreateAsync(KUpsertNodeRequest request, int userId, int knowledgeId, List<KNodeEntity> upserted);
        void ProcessUpdate(KUpsertNodeRequest request, Dictionary<int, KNodeEntity> existingNodes, List<KNodeEntity> upserted);
        void ProcessMove(KUpsertNodeRequest request, Dictionary<int, KNodeEntity> existingNodes, List<KNodeEntity> upserted);
        Task ProcessMoveCrossAsync(KUpsertNodeRequest request, Dictionary<int, KNodeEntity> existingNodes, List<KNodeEntity> upserted);
        void ProcessDelete(KUpsertNodeRequest request, Dictionary<int, KNodeEntity> existingNodes, List<KNodeEntity> upserted);
        void ProcessRestore(KUpsertNodeRequest request, Dictionary<int, KNodeEntity> existingNodes, List<KNodeEntity> upserted);

        Task<List<KNodeEntity>> GetAllDescendantsAsync(int parentNodeId);

        // PATH SYNC
        Task SyncPathIdsAsync(List<KNodeEntity> upserted);
        Task RebuildPathIdsAsync(KNodeEntity node);
        Task UpdateDescendantPathIdsAsync(int parentId, string oldPathIds, string newPathIds);
    }
}
