using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    /// <summary>
    /// Repository interface for workspace list operations (ws.workspaces table)
    /// </summary>
    public interface IWorkspaceListRepository
    {
        Task<ResultOptions> GetWorkspacesAsync(WorkspaceFilterOptions filterOptions);
        Task<ResultOptions> GetWorkspaceById(int workspaceId);
        Task<ResultOptions> CreateWorkspaceAsync(Workspace workspace);
        Task<ResultOptions> UpdateWorkspaceAsync(Workspace workspace);
        Task<ResultOptions> DeleteWorkspacesBatchAsync(List<int> workspaceIds, bool isHardDelete = false);
        Task<ResultOptions> UndoDeleteWorkspacesBatchAsync(List<int> workspaceIds);
    }
}
