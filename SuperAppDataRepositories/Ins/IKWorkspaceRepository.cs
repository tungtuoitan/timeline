using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IKWorkspaceRepository
    {
        Task<KWorkspaceWithTree?> GetWorkspaceTreeAsync(int workspaceId, int userId, KWorkspaceFilterOptions? filterOptions = null);
        Task<KWorkspace?> GetWorkspaceByIdAsync(int workspaceId, int userId);
        Task<List<KWorkspace>> GetAllWorkspacesByUserIdAsync(int userId, FilterOptions? filterOptions = null);

        /// <summary>Moves items by workspace_item IDs (using sp_MoveWorkspaceItems)</summary>
        //Task<ResultOptions> MoveItemsAsync(int sourceWorkspaceId, List<int> itemIds, int? targetParentId, int? targetWorkspaceId);

        /// <summary>Deletes items by workspace_item IDs (using sp_DeleteWorkspaceItems)</summary>
        Task<ResultOptions> DeleteItemsAsync(int workspaceId, List<int> itemIds);
    }
}
