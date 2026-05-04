using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
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

        /// <summary>
        /// Gets workspace tree with all items (V2 - with full entity data)
        /// Returns unified WorkspaceDTO with flat list of items
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>Unified WorkspaceDTO with workspace data + flatData</returns>
        Task<WorkspaceDTO> GetWorkspaceTreeV2Async(int workspaceId, int userId, WorkspaceFilterOptions? filterOptions = null);

        /// <summary>
        /// Gets all workspaces for a user with optional filters
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="filterOptions">Filter options (status, dates, etc.)</param>
        /// <returns>List of workspace summaries</returns>
        Task<List<WsResponse>> GetAllUserWorkspacesAsync(int userId, FilterOptions? filterOptions = null);

        /// <summary>
        /// Creates a new folder in a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="request">Create folder request</param>
        /// <returns>Result options with created folder data</returns>
        Task<ResultOptions> UpsertFolderAsync(int workspaceId, int userId, UpsertFolderRequest request);

        /// <summary>
        /// Moves multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <param name="workspaceId">Source workspace ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="request">Move items request</param>
        /// <returns>Result options with affected count</returns>
        Task<ResultOptions> MoveItemsAsync(int workspaceId, int userId, MoveItemsRequest request);

        /// <summary>
        /// Deletes multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="request">Delete items request</param>
        /// <returns>Result options with affected count</returns>
        Task<ResultOptions> DeleteItemsAsync(int workspaceId, int userId, DeleteItemsRequest request);

        /// <summary>
        /// Adds an item (folder/note/file) to a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="request">Add item request</param>
        /// <returns>Result options with created relationship data</returns>
        Task<ResultOptions> AddItemToWorkspaceAsync(int workspaceId, int userId, AddItemToWorkspaceRequest request);

        /// <summary>
        /// Batch upsert workspace items (soft delete/restore only)
        /// Pattern: 100% follows NoteService.UpsertNotesAsync
        /// </summary>
        /// <param name="requests">List of workspace item upsert requests</param>
        /// <param name="userId">User ID for access validation</param>
        /// <returns>Result options with upserted items</returns>
        Task<ResultOptions> UpsertWorkspaceItemsAsync(List<UpsertWorkspaceItemRequest> requests, int userId);
    }
}
