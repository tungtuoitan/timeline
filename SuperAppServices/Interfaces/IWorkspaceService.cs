using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for workspace operations
    /// </summary>
    public interface IWorkspaceService
    {
        /// <summary>
        /// Gets workspace tree with all items (tags, notes, files)
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>Workspace with hierarchical tree structure</returns>
        Task<WorkspaceWithTreeResponse> GetWorkspaceTreeAsync(int workspaceId, int userId);
    }
}
