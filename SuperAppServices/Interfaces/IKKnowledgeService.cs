using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IKKnowledgeService
    {
        Task<KKnowledgeDTO> GetKnowledgeTreeAsync(int knowledgeId, int userId);
        Task<List<KKnowledgeSummary>> GetAllKnowledgesAsync(int userId, FilterOptions? filterOptions = null);
        Task<ResultOptions> DeleteNodesAsync(int knowledgeId, int userId, KDeleteNodesRequest request);

        /// <summary>
        /// Hard-deletes a single shortcut node row.
        /// Validates ownership and that the node is a shortcut.
        /// </summary>
        Task<ResultOptions> DeleteShortcutAsync(int knowledgeId, int nodeId, int userId);

        Task<ResultOptions> CreateKnowledgeAsync(KUpsertKnowledgeRequest request);
        Task<ResultOptions> UpdateKnowledgeAsync(int id, KUpsertKnowledgeRequest request);
        Task<ResultOptions> SoftDeleteKnowledgeAsync(int id, int userId);
    }
}

