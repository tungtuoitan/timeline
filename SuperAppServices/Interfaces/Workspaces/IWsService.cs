using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for workspace list operations
    /// </summary>
    public interface IWsService
    {
        /// <summary>
        /// Get all workspaces with optional filters
        /// </summary>
        /// <param name="filterOptions">Filter options including userId, searchText, tagIds, status, dates, pagination, etc.</param>
        /// <returns>ResultOptions containing list of workspaces with totalCount</returns>
        Task<ResultOptions> GetWorkspacesAsync(WsFilterOptions filterOptions);

        /// <summary>
        /// Get workspace by ID
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <returns>ResultOptions containing workspace details</returns>
        Task<ResultOptions> GetWorkspaceByIdAsync(int workspaceId);

        /// <summary>
        /// Batch create or update multiple workspaces (upsert)
        /// For single workspace operations, pass a list with 1 element
        /// </summary>
        /// <param name="requests">List of workspace upsert requests</param>
        /// <returns>ResultOptions containing batch operation results</returns>
        Task<ResultOptions> UpsertWorkspacesBatchAsync(List<UpsertWorkspaceRequest> requests);

        /// <summary>
        /// Delete workspaces by IDs with cascade to all items
        /// </summary>
        /// <param name="workspaceIds">Comma-separated workspace IDs (e.g., "1,2,3")</param>
        /// <param name="isHardDelete">Hard delete flag</param>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> DeleteWorkspacesAsync(string workspaceIds);
    }
}
