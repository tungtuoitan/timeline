using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    /// <summary>
    /// Repository interface for workspace list operations (ws.workspaces table)
    /// </summary>
    public interface IWsRepository
    {
        Task<ResultOptions> GetWorkspacesAsync(WsFilterOptions filterOptions);
        Task<ResultOptions> GetWorkspaceById(int workspaceId);
        Task<ResultOptions> UpsertWorkspaceAsync(Workspace workspace);
        Task<ResultOptions> UpdateWorkspaceNameAsync(int workspaceId, string name);
        Task<ResultOptions> DeleteWorkspacesCascadeAsync(string workspaceIds);
        Task<ResultOptions> UndoDeleteWorkspacesBatchAsync(List<int> workspaceIds);
    }
}
