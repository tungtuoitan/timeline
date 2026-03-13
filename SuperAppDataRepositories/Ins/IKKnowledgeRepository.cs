using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IKKnowledgeRepository
    {
        Task<KKnowledgeWithTree?> GetKnowledgeTreeAsync(int knowledgeId, int userId);
        Task<KKnowledge?> GetKnowledgeByIdAsync(int knowledgeId, int userId);
        Task<List<KKnowledge>> GetAllKnowledgesByUserIdAsync(int userId, FilterOptions? filterOptions = null);

        Task<KKnowledge> CreateAsync(KUpsertKnowledgeRequest request);
        Task<KKnowledge?> UpdateAsync(int id, KUpsertKnowledgeRequest request);
        Task<bool> SoftDeleteAsync(int id, int userId);

        /// <summary>Hard-deletes nodes and descendants via [k].[sp_DeleteNodes]</summary>
        Task<ResultOptions> DeleteNodesAsync(int knowledgeId, List<int> nodeIds);
    }
}

