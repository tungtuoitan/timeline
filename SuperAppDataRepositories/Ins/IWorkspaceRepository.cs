using SuperAppModels.DTOs;
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
        /// Creates a new folder in a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID (folder owner)</param>
        /// <param name="name">Folder name</param>
        /// <param name="description">Folder description</param>
        /// <param name="color">Folder color (hex format)</param>
        /// <param name="icon">Folder icon</param>
        /// <param name="parentFolderId">Parent folder ID (null = root level)</param>
        /// <returns>Created folder</returns>
        Task<ResultOptions> UpsertFolderAsync(int workspaceId, int userId, int? folderId, string name, string? description, string? color, string? icon, int? parentFolderId);

        /// <summary>
        /// Moves multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <param name="sourceWorkspaceId">Source workspace ID</param>
        /// <param name="items">List of items to move (type + id)</param>
        /// <param name="targetFolderId">Target folder ID (null = root level)</param>
        /// <param name="targetWorkspaceId">Target workspace ID (null = same workspace)</param>
        /// <returns>Result with affected count</returns>
        Task<ResultOptions> MoveItemsAsync(int sourceWorkspaceId, List<(byte ItemType, int ItemId)> items, int? targetFolderId, int? targetWorkspaceId);

        /// <summary>
        /// Deletes multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="items">List of items to delete (type + id)</param>
        /// <returns>Result with affected count</returns>
        Task<ResultOptions> DeleteItemsAsync(int workspaceId, List<(byte ItemType, int ItemId)> items);
    }
}
