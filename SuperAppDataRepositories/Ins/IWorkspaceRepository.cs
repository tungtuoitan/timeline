using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    /// <summary>
    /// Repository interface for workspace data access
    /// </summary>
    public interface IWorkspaceRepository
    {
        /// <summary>
        /// Gets the complete workspace tree (folders, notes, and files) with hierarchy
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID for access validation</param>
        /// <returns>Workspace with hierarchical tree structure</returns>
        Task<WorkspaceWithTree?> GetWorkspaceTreeAsync(int workspaceId, int userId);
    }
}
