using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Interface for kworkspace item operations with action-based batch processing
    /// </summary>
    public interface IKWorkspaceItemService
    {
        /// <summary>
        /// Batch upsert workspace items with transaction management and action-based validation
        /// Supports 6 actions: Create, Add, Move, Update, Delete, Restore
        /// Pattern: Preload → Validate → Process → Single SaveChanges
        /// </summary>
        /// <param name="requests">List of workspace item requests with explicit actions</param>
        /// <param name="userId">User ID from JWT claims</param>
        /// <param name="workspaceId">Workspace ID from route parameter</param>
        /// <returns>Result with upserted workspace items or error details</returns>
        Task<ResultOptions> UpsertWorkspaceItemsAsync(
            List<KUpsertWorkspaceItemRequest> requests,
            int userId,
            int workspaceId);
    }
}
