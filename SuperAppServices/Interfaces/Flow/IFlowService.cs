using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    public interface IFlowService
    {
        Task<ResultOptions> GetEdgesAsync(int userId);
        Task<ResultOptions> UpsertEdgesAsync(List<UpsertFlowEdgeRequest> requests, int userId);
        Task<ResultOptions> GetNodePositionsAsync(int userId, List<int>? nodeIds, string? nodeType);
        Task<ResultOptions> UpsertNodePositionsAsync(List<UpsertFlowNodePositionRequest> requests, int userId);
    }
}
