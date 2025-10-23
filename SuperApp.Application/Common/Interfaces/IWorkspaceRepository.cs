using SuperAppModels.Models;

namespace SuperApp.Application.Common.Interfaces
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

        /// <summary>
        /// Gets workspace tree (tags, notes, files) for a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID for access check</param>
        /// <returns>List of workspace tree items</returns>
        Task<List<WorkspaceTreeItem>> GetWorkspaceTreeAsync(int workspaceId, int userId);

        /// <summary>
        /// Updates workspace item metadata (label, notes, color, icon, sort_order)
        /// </summary>
        /// <param name="itemId">The item ID to update</param>
        /// <param name="userId">User ID for access validation</param>
        /// <param name="label">Optional new label</param>
        /// <param name="notes">Optional new notes</param>
        /// <param name="color">Optional new color in hex format (#RRGGBB)</param>
        /// <param name="icon">Optional new icon</param>
        /// <param name="sortOrder">Optional new sort order</param>
        /// <returns>Updated workspace item</returns>
        Task<WorkspaceItem> UpdateWorkspaceItemAsync(
            long itemId,
            int userId,
            string? label = null,
            string? notes = null,
            string? color = null,
            string? icon = null,
            int? sortOrder = null);

        /// <summary>
        /// Moves workspace item to a different parent tag
        /// </summary>
        /// <param name="itemId">The item ID to move</param>
        /// <param name="userId">User ID for access validation</param>
        /// <param name="newParentTagId">New parent tag ID (null for root level)</param>
        /// <param name="sortOrder">Optional sort order in new location</param>
        /// <returns>Updated workspace item</returns>
        Task<WorkspaceItem> MoveWorkspaceItemAsync(
            long itemId,
            int userId,
            int? newParentTagId,
            int? sortOrder = null);
    }
}
