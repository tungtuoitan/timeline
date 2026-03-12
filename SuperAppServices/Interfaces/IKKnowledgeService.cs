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
    }
}
