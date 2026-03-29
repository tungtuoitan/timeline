using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppDataRepositories.Ins
{
    public interface IFlowRepository
    {
        /// <summary>Get all non-deleted edges owned by user.</summary>
        Task<ResultOptions> GetEdgesAsync(int userId);

        /// <summary>Batch upsert edges. Soft-delete by passing DeletedAt.</summary>
        Task<ResultOptions> UpsertEdgesAsync(List<UpsertFlowEdgeRequest> requests, int userId);

        /// <summary>Get saved node positions for a user (optionally filtered by nodeIds).</summary>
        Task<ResultOptions> GetNodePositionsAsync(int userId, List<int>? nodeIds = null, string? nodeType = null);

        /// <summary>Batch upsert node positions (insert or update on unique key).</summary>
        Task<ResultOptions> UpsertNodePositionsAsync(List<UpsertFlowNodePositionRequest> requests, int userId);
    }
}
