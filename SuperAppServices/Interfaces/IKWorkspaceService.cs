using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IKWorkspaceService
    {
        Task<KWorkspaceDTO> GetWorkspaceTreeV2Async(int workspaceId, int userId, WorkspaceFilterOptions? filterOptions = null);
        Task<List<WsResponse>> GetAllUserWorkspacesAsync(int userId, FilterOptions? filterOptions = null);
        Task<ResultOptions> MoveItemsAsync(int workspaceId, int userId, KMoveItemsRequest request);
        Task<ResultOptions> DeleteItemsAsync(int workspaceId, int userId, KDeleteItemsRequest request);
    }
}
