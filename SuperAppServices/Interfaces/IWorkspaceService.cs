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
        /// Gets all workspaces for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of workspace summaries</returns>
        Task<List<WorkspaceListResponse>> GetAllUserWorkspacesAsync(int userId);

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
    }
}
