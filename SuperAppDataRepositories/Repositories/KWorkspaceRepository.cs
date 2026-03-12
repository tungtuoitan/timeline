using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using System.Data;
using System.Text.Json;

namespace SuperAppDataRepositories.Repositories
{
    public class KWorkspaceRepository : IKWorkspaceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KWorkspaceRepository> _logger;

        public KWorkspaceRepository(
            ApplicationDbContext context,
            ILogger<KWorkspaceRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the complete workspace with flat list of nodes.
        /// kws.workspace_items is self-contained — no joins to external entity tables.
        /// </summary>
        public async Task<KWorkspaceWithTree?> GetWorkspaceTreeAsync(int workspaceId, int userId, KWorkspaceFilterOptions? filterOptions = null)
        {
            try
            {
                _logger.LogInformation("Getting kworkspace tree for workspaceId: {WorkspaceId}", workspaceId);

                var workspace = await _context.KWorkspaces
                    .Where(w => w.Id == workspaceId)
                    .FirstOrDefaultAsync();

                if (workspace == null)
                {
                    _logger.LogWarning("KWorkspace not found: {WorkspaceId}", workspaceId);
                    return null;
                }

                var items = await _context.KWorkspaceItems
                    .Where(i => i.WorkspaceId == workspaceId 
                    //&& i.DeletedAt == null
                    )
                    .ToListAsync();

                _logger.LogInformation("Found {Count} items in kworkspace {WorkspaceId}", items.Count, workspaceId);

                return new KWorkspaceWithTree
                {
                    WorkspaceId = workspace.Id,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    UserId = workspace.UserId,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    Items = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kworkspace tree for workspaceId: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        public async Task<KWorkspace?> GetWorkspaceByIdAsync(int workspaceId, int userId)
        {
            try
            {
                return await _context.KWorkspaces
                    .Where(w => w.Id == workspaceId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kworkspace by ID: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        public async Task<List<KWorkspace>> GetAllWorkspacesByUserIdAsync(int userId, FilterOptions? filterOptions = null)
        {
            try
            {
                var query = _context.KWorkspaces.Where(w => w.UserId == userId);

                if (filterOptions != null)
                {
                    if (filterOptions.StatusCodes != null && filterOptions.StatusCodes.Any())
                        query = query.Where(w => w.StatusCode != null && filterOptions.StatusCodes.Contains(w.StatusCode));

                    if (!string.IsNullOrEmpty(filterOptions.DeletedAt))
                    {
                        if (filterOptions.DeletedAt == "null")
                            query = query.Where(w => w.DeletedAt == null);
                        else if (filterOptions.DeletedAt == "notNull")
                            query = query.Where(w => w.DeletedAt != null);
                    }

                    if (filterOptions.CreatedFrom.HasValue)
                        query = query.Where(w => w.CreatedAt >= filterOptions.CreatedFrom.Value);

                    if (filterOptions.CreatedTo.HasValue)
                        query = query.Where(w => w.CreatedAt <= filterOptions.CreatedTo.Value);

                    if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                        query = query.Where(w => w.Name.Contains(filterOptions.SearchText) ||
                                                 (w.Description != null && w.Description.Contains(filterOptions.SearchText)));
                }

                return await query.OrderBy(w => w.CreatedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kworkspaces for userId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Moves workspace items by their workspace_item IDs using sp_MoveWorkspaceItems
        /// </summary>
        //public async Task<ResultOptions> MoveItemsAsync(int sourceWorkspaceId, List<int> itemIds, int? targetParentId, int? targetWorkspaceId)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Moving {Count} items from workspace {SourceWorkspaceId}", itemIds.Count, sourceWorkspaceId);

        //        var itemIdsJson = JsonSerializer.Serialize(itemIds);

        //        var sourceWorkspaceIdParam = new SqlParameter("@SourceWorkspaceId", sourceWorkspaceId);
        //        var itemsParam = new SqlParameter("@ItemIds", SqlDbType.NVarChar, -1) { Value = itemIdsJson };
        //        var targetParentIdParam = new SqlParameter("@TargetParentId", SqlDbType.Int) { Value = (object?)targetParentId ?? DBNull.Value };
        //        var targetWorkspaceIdParam = new SqlParameter("@TargetWorkspaceId", SqlDbType.Int) { Value = (object?)targetWorkspaceId ?? DBNull.Value };

        //        await _context.Database.ExecuteSqlRawAsync(
        //            "EXEC [kws].[sp_MoveWorkspaceItems] @SourceWorkspaceId, @ItemIds, @TargetParentId, @TargetWorkspaceId",
        //            sourceWorkspaceIdParam, itemsParam, targetParentIdParam, targetWorkspaceIdParam);

        //        _logger.LogInformation("Successfully moved items from workspace {SourceWorkspaceId}", sourceWorkspaceId);

        //        return new ResultOptions { Success = true, Message = "Items moved successfully", Status = 200 };
        //    }
        //    catch (SqlException ex)
        //    {
        //        _logger.LogError(ex, "SQL error while moving items from workspace {SourceWorkspaceId}", sourceWorkspaceId);
        //        return new ResultOptions { Success = false, Message = $"Database error: {ex.Message}", Status = 500 };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error moving items from workspace {SourceWorkspaceId}", sourceWorkspaceId);
        //        return new ResultOptions { Success = false, Message = $"Failed to move items: {ex.Message}", Status = 500 };
        //    }
        //}

        /// <summary>
        /// Deletes workspace items by their workspace_item IDs using sp_DeleteWorkspaceItems
        /// </summary>
        public async Task<ResultOptions> DeleteItemsAsync(int workspaceId, List<int> itemIds)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} items from workspace {WorkspaceId}", itemIds.Count, workspaceId);

                var itemIdsJson = JsonSerializer.Serialize(itemIds);

                var workspaceIdParam = new SqlParameter("@iv_workspace_id", workspaceId);
                var itemsParam = new SqlParameter("@iv_item_ids", SqlDbType.NVarChar, -1) { Value = itemIdsJson };
                var deletedCountParam = new SqlParameter("@ov_deleted_count", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [kws].[sp_DeleteWorkspaceItems] @iv_workspace_id, @iv_item_ids, @ov_deleted_count OUTPUT",
                    workspaceIdParam, itemsParam, deletedCountParam);

                var deletedCount = (int)deletedCountParam.Value;
                _logger.LogInformation("Successfully deleted {Count} items (including descendants)", deletedCount);

                return new ResultOptions { Success = true, Message = "Items deleted successfully", Reference = deletedCount.ToString(), Status = 200 };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while deleting items from workspace {WorkspaceId}", workspaceId);
                return new ResultOptions { Success = false, Message = $"Database error: {ex.Message}", Status = 500 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting items from workspace {WorkspaceId}", workspaceId);
                return new ResultOptions { Success = false, Message = $"Failed to delete items: {ex.Message}", Status = 500 };
            }
        }
    }
}
