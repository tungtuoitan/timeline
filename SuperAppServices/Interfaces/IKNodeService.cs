using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    public interface IKNodeService
    {
        Task<ResultOptions> UpsertNodesAsync(List<KUpsertNodeRequest> requests, int userId, int knowledgeId);
    }
}
