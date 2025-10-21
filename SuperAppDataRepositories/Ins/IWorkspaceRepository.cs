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

        /// <summary>
        /// Validates that a user has the required access level to a workspace
        /// </summary>
        /// <param name="workspaceId">The workspace ID to check</param>
        /// <param name="userId">The user ID requesting access</param>
        /// <param name="requiredRoles">Array of acceptable roles (e.g., "owner", "editor", "viewer")</param>
        /// <returns>Task that completes if access is granted</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if user doesn't have required access</exception>
        Task ValidateUserAccessAsync(int workspaceId, int userId, string[] requiredRoles);

        /// <summary>
        /// Adds an item (tag or note) to a workspace
        /// </summary>
        /// <param name="item">The workspace item to add</param>
        /// <returns>The created workspace item with generated ItemId</returns>
        /// <exception cref="ArgumentException">Thrown if item is invalid or duplicate</exception>
        Task<WorkspaceItem> AddItemToWorkspaceAsync(WorkspaceItem item);

        /// <summary>
        /// Checks if a workspace item already exists
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <param name="parentTagId">The parent tag ID (nullable)</param>
        /// <param name="childType">The child type (tag or note)</param>
        /// <param name="childId">The child ID</param>
        /// <returns>True if item exists, false otherwise</returns>
        Task<bool> ItemExistsAsync(int workspaceId, int? parentTagId, string childType, int childId);

        /// <summary>
        /// Gets all items in a workspace
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <returns>List of workspace items</returns>
        Task<List<WorkspaceItem>> GetWorkspaceItemsAsync(int workspaceId);

        /// <summary>
        /// Removes an item from a workspace
        /// </summary>
        /// <param name="itemId">The item ID to remove</param>
        /// <returns>True if removed successfully, false if not found</returns>
        Task<bool> RemoveItemFromWorkspaceAsync(int itemId);
    }
}
