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
        Task<ResultOptions> CreateFolderAsync(int workspaceId, int userId, string name, string? description, string? color, string? icon, int? parentFolderId);
    }
}
