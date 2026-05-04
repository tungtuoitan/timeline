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
        /// Supports filtering workspace_items by status code and deleted status
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID for access validation</param>
        /// <param name="filterOptions">Filter options for workspace_items (status, deleted status)</param>
        /// <returns>Workspace with hierarchical tree structure</returns>
        Task<WorkspaceWithTree?> GetWorkspaceTreeAsync(int workspaceId, int userId, WorkspaceFilterOptions? filterOptions = null);

        /// <summary>
        /// Gets workspace by ID
        /// </summary>
        Task<Workspace?> GetWorkspaceByIdAsync(int workspaceId, int userId);

        /// <summary>
        /// Gets all workspaces for a user with optional filters
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="filterOptions">Filter options (status, dates, etc.)</param>
        /// <returns>List of workspaces</returns>
        Task<List<Workspace>> GetAllWorkspacesByUserIdAsync(int userId, FilterOptions? filterOptions = null);

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
        Task<ResultOptions> MoveItemsAsync(int sourceWorkspaceId, List<(byte EntityType, int EntityId)> items, int? targetParentId, int? targetWorkspaceId);

        /// <summary>
        /// Deletes multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="items">List of items to delete (type + id)</param>
        /// <returns>Result with affected count</returns>
        Task<ResultOptions> DeleteItemsAsync(int workspaceId, List<(byte EntityType, int EntityId)> items, bool isHardDelete = false);

        /// <summary>
        /// Adds an item (folder/note/file) to a workspace
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="request">Add item request</param>
        /// <returns>Result with created workspace_items row</returns>
        Task<ResultOptions> AddItemToWorkspaceAsync(int workspaceId, int userId, AddItemToWorkspaceRequest request);

        /// <summary>
        /// Batch upsert workspace items (soft delete/restore only)
        /// Pattern: 100% follows NoteRepository.UpsertNotesAsync
        /// </summary>
        /// <param name="requests">List of workspace item upsert requests</param>
        /// <param name="userId">User ID for access validation</param>
        /// <returns>Result options with upserted items</returns>
        Task<ResultOptions> UpsertWorkspaceItemsAsync(List<UpsertWorkspaceItemRequest> requests, int userId);

        /// <summary>
        /// Updates only the name of a folder identified by its workspace_items.id
        /// </summary>
        Task<ResultOptions> UpdateFolderNameByWorkspaceItemIdAsync(int workspaceItemId, string name);
    }
}
