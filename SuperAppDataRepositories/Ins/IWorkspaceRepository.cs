using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
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

        /// <summary>
        /// Gets workspace by ID
        /// </summary>
        Task<Workspace?> GetWorkspaceByIdAsync(int workspaceId, int userId);

        /// <summary>
        /// Gets all workspaces for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of workspaces</returns>
        Task<List<Workspace>> GetAllWorkspacesByUserIdAsync(int userId);

        /// <summary>
        /// Creates or updates a folder in a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID (folder owner)</param>
        /// <param name="request">Folder upsert request data</param>
        /// <returns>Result with folder ID</returns>
        Task<ResultOptions> UpsertFolderAsync(int workspaceId, int userId, UpsertFolderRequest request);

        /// <summary>
        /// Moves multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <param name="sourceWorkspaceId">Source workspace ID</param>
        /// <param name="items">List of items to move (type + id)</param>
        /// <param name="targetParentId">Target parent folder ID (null = root level)</param>
        /// <param name="targetWorkspaceId">Target workspace ID (null = same workspace)</param>
        /// <returns>ResultOptions with affected count</returns>
        Task<ResultOptions> MoveItemsAsync(int sourceWorkspaceId, List<(byte ItemType, int ItemId)> items, int? targetParentId, int? targetWorkspaceId);

        /// <summary>
        /// Deletes multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="items">List of items to delete (type + id)</param>
        /// <returns>Result with affected count</returns>
        Task<ResultOptions> DeleteItemsAsync(int workspaceId, List<(byte ItemType, int ItemId)> items, bool isHardDelete = false);

        /// <summary>
        /// Adds an item (folder/note/file) to a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="request">Add item request</param>
        /// <returns>Result with created workspace_items row</returns>
        Task<ResultOptions> AddItemToWorkspaceAsync(int workspaceId, int userId, AddItemToWorkspaceRequest request);
    }
}
