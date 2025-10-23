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
    /// Removes an item from a workspace (soft delete)
    /// Also removes all descendants (children, grandchildren, etc.)
    /// </summary>
    /// <param name="itemId">The item ID to remove</param>
    /// <param name="deleteDescendants">Whether to delete all descendants (default: true)</param>
    /// <returns>True if removed successfully, false if not found</returns>
    Task<bool> RemoveItemFromWorkspaceAsync(int itemId, bool deleteDescendants = true);

        /// <summary>
        /// Gets the complete workspace tree (tags, notes, and files) with hierarchy
        /// This is the new unified tree endpoint supporting all entity types
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <param name="userId">User ID for access validation</param>
        /// <returns>List of workspace tree items with polymorphic metadata</returns>
        Task<List<WorkspaceTreeItem>> GetWorkspaceTreeAsync(int workspaceId, int userId);

        /// <summary>
        /// Gets the workspace tag tree (backward compatibility - tags only)
        /// DEPRECATED: Use GetWorkspaceTreeAsync for new development
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <param name="userId">User ID for access validation</param>
        /// <returns>List of tags in hierarchical structure</returns>
        [Obsolete("Use GetWorkspaceTreeAsync instead. This method will be removed in v2.0")]
        Task<List<WorkspaceTagTree>> GetWorkspaceTagTreeAsync(int workspaceId, int userId);

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
        /// <exception cref="UnauthorizedAccessException">If user doesn't have editor/owner access</exception>
        /// <exception cref="KeyNotFoundException">If item not found</exception>
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
        /// <exception cref="UnauthorizedAccessException">If user doesn't have editor/owner access</exception>
        /// <exception cref="KeyNotFoundException">If item not found</exception>
        /// <exception cref="ArgumentException">If move would create a cycle</exception>
        Task<WorkspaceItem> MoveWorkspaceItemAsync(
            long itemId,
            int userId,
            int? newParentTagId,
            int? sortOrder = null);
    }
}