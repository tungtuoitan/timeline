using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    /// <summary>
    /// Repository interface for workspace data access
    /// </summary>
    public interface IWorkspaceRepository
    {
        /// <summary>
        /// Gets workspace by ID with access validation
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID for access check</param>
        /// <returns>Workspace if found and user has access, null otherwise</returns>
        Task<Workspace?> GetWorkspaceByIdAsync(int workspaceId, int userId);

        /// <summary>
        /// Gets all workspaces for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="includeArchived">Include archived workspaces</param>
        /// <returns>List of workspaces</returns>
        Task<List<Workspace>> GetUserWorkspacesAsync(int userId, bool includeArchived = false);
    }
}
