using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for workspace list operations
    /// </summary>
    public interface IWorkspaceListService
    {
        /// <summary>
        /// Get all workspaces with optional filters
        /// </summary>
        /// <param name="userId">User ID to filter workspaces</param>
        /// <param name="getAll">Get all workspaces or only active</param>
        /// <param name="searchText">Search text filter</param>
        /// <returns>ResultOptions containing list of workspaces</returns>
        Task<ResultOptions> GetWorkspacesAsync(int userId, bool getAll, string? searchText);

        /// <summary>
        /// Get workspace by ID
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <returns>ResultOptions containing workspace details</returns>
        Task<ResultOptions> GetWorkspaceByIdAsync(int workspaceId);

        /// <summary>
        /// Create or update workspace (upsert)
        /// </summary>
        /// <param name="request">Workspace upsert request</param>
        /// <returns>ResultOptions containing created or updated workspace</returns>
        Task<ResultOptions> UpsertWorkspaceAsync(UpsertWorkspaceRequest request);

        /// <summary>
        /// Delete workspaces by IDs with cascade to all items
        /// </summary>
        /// <param name="workspaceIds">Comma-separated workspace IDs (e.g., "1,2,3")</param>
        /// <param name="isHardDelete">Hard delete flag</param>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> DeleteWorkspacesAsync(string workspaceIds, bool isHardDelete = false);

        /// <summary>
        /// Restore deleted workspaces by setting deleted_at to null
        /// </summary>
        /// <param name="workspaceIds">List of workspace IDs to restore</param>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> UndoDeleteWorkspacesAsync(List<int> workspaceIds);
    }
}
