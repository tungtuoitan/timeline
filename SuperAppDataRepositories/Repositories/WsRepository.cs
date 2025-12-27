using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using System.Data;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for workspace list data access (ws.workspaces table)
    /// </summary>
    public class WsRepository : IWsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WsRepository> _logger;
         
        public WsRepository(
            ApplicationDbContext context,
            ILogger<WsRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all workspaces with comprehensive filtering options
        /// </summary>
        public async Task<ResultOptions> GetWorkspacesAsync(WsFilterOptions filterOptions)
        {
            try
            {
                if (filterOptions == null)
                    throw new ArgumentNullException(nameof(filterOptions));

                _logger.LogInformation(
                    "Getting workspaces with filters - UserId: {UserId}, SearchText: {SearchText}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                    filterOptions.UserId, 
                    filterOptions.SearchText, 
                    filterOptions.PageNumber, filterOptions.PageSize);

                var query = _context.Workspaces
                    .AsNoTracking();
                    // ✅ Include deleted workspaces - Frontend will handle display logic

                // Filter by user ID if provided
                if (filterOptions.UserId.HasValue)
                {
                    query = query.Where(w => w.UserId == filterOptions.UserId.Value);
                }

                // Filter by search text if provided
                if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                {
                    query = query.Where(w => w.Name.Contains(filterOptions.SearchText) || 
                                           (w.Description != null && w.Description.Contains(filterOptions.SearchText)));
                }

                // Sorting
                query = filterOptions.SortBy?.ToLower() switch
                {
                    "name" => filterOptions.SortOrder?.ToLower() == "asc" 
                        ? query.OrderBy(w => w.Name) 
                        : query.OrderByDescending(w => w.Name),
                    "updatedat" => filterOptions.SortOrder?.ToLower() == "asc" 
                        ? query.OrderBy(w => w.UpdatedAt) 
                        : query.OrderByDescending(w => w.UpdatedAt),
                    _ => filterOptions.SortOrder?.ToLower() == "asc" 
                        ? query.OrderBy(w => w.CreatedAt) 
                        : query.OrderByDescending(w => w.CreatedAt)
                };

                // Pagination
                if (filterOptions.PageNumber.HasValue && filterOptions.PageSize.HasValue)
                {
                    var pageNumber = filterOptions.PageNumber.Value < 1 ? 1 : filterOptions.PageNumber.Value;
                    var pageSize = filterOptions.PageSize.Value < 1 ? 20 : 
                                  filterOptions.PageSize.Value > 100 ? 100 : filterOptions.PageSize.Value;
                    
                    query = query
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize);
                }

                var workspaces = await query.ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} workspaces", workspaces.Count);
                
                return new ResultOptions
                {
                    Success = true,
                    Message = "Workspaces retrieved successfully",
                    Data = workspaces.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting workspaces with filters");
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error while retrieving workspaces",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspaces with filters");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Gets workspace by ID
        /// </summary>
        public async Task<ResultOptions> GetWorkspaceById(int workspaceId)
        {
            try
            {
                _logger.LogInformation("Getting workspace by ID: {WorkspaceId}", workspaceId);

                var workspace = await _context.Workspaces
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);
                    // ✅ Include deleted workspace - Frontend will handle display logic

                if (workspace == null)
                {
                    _logger.LogWarning("Workspace not found with ID: {WorkspaceId}", workspaceId);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"Workspace not found with ID: {workspaceId}",
                        Status = 404
                    };
                }
                else
                {
                    _logger.LogInformation("Successfully retrieved workspace with ID: {WorkspaceId}", workspaceId);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Workspace retrieved successfully",
                        Object = workspace,
                        Status = 200
                    };
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting workspace by ID: {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error while retrieving workspace",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace by ID: {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Creates or updates a workspace (upsert)
        /// </summary>
        public async Task<ResultOptions> UpsertWorkspaceAsync(Workspace workspace)
        {
            try
            {
                if (workspace == null)
                    throw new ArgumentNullException(nameof(workspace));

                bool isUpdate = workspace.Id > 0;

                if (isUpdate)
                {
                    // Find workspace (including deleted ones for restore)
                    var existingWorkspace = await _context.Workspaces
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(w => w.Id == workspace.Id);

                    // Determine operation type for logging
                    var operation = workspace.DeletedAt.HasValue ? "Soft deleting" :
                                   (existingWorkspace?.DeletedAt.HasValue == true ? "Restoring" : "Updating");

                    _logger.LogInformation("{Operation} workspace ID: {WorkspaceId}, Name: '{Name}', UserId: {UserId}",
                        operation, workspace.Id, workspace.Name, workspace.UserId);

                    if (existingWorkspace == null)
                    {
                        _logger.LogWarning("Workspace not found for update with ID: {WorkspaceId}", workspace.Id);
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"Workspace with ID {workspace.Id} not found",
                            Status = 404
                        };
                    }

                    // Validate UserId exists before update
                    var userExists = await _context.Users.AnyAsync(u => u.Id == workspace.UserId);
                    if (!userExists)
                    {
                        _logger.LogError("User not found with ID: {UserId}", workspace.UserId);
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"User with ID {workspace.UserId} not found",
                            Status = 400
                        };
                    }

                    // Update properties
                    existingWorkspace.Name = workspace.Name;
                    existingWorkspace.Description = workspace.Description;
                    existingWorkspace.UserId = workspace.UserId;
                    existingWorkspace.DeletedAt = workspace.DeletedAt;  // Handle soft delete/restore
                    existingWorkspace.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully updated workspace with ID: {WorkspaceId}", workspace.Id);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Workspace updated successfully",
                        Object = existingWorkspace,
                        Status = 200
                    };
                }
                else
                {
                    // CREATE new workspace
                    _logger.LogInformation("Creating workspace with Name: '{Name}', UserId: {UserId}",
                        workspace.Name, workspace.UserId);

                    // Validate UserId exists before create
                    var userExists = await _context.Users.AnyAsync(u => u.Id == workspace.UserId);
                    if (!userExists)
                    {
                        _logger.LogError("User not found with ID: {UserId}", workspace.UserId);
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"User with ID {workspace.UserId} not found",
                            Status = 400
                        };
                    }

                    // Ensure timestamps
                    workspace.CreatedAt = DateTime.UtcNow;
                    workspace.UpdatedAt = null;
                    workspace.DeletedAt = null;

                    _context.Workspaces.Add(workspace);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully created workspace with ID: {WorkspaceId}", workspace.Id);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Workspace created successfully",
                        Object = workspace,
                        Status = 201
                    };
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while upserting workspace ID: {WorkspaceId}", workspace.Id);
                return new ResultOptions
                {
                    Success = false,
                    Message = workspace.Id > 0 
                        ? $"Workspace {workspace.Id} was modified by another user" 
                        : "Concurrency conflict occurred while creating workspace",
                    Status = 409
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while upserting workspace ID: {WorkspaceId}, UserId: {UserId}", 
                    workspace.Id, workspace.UserId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while saving workspace",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting workspace ID: {WorkspaceId}, Name: '{Name}'", 
                    workspace.Id, workspace.Name);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Deletes multiple workspaces with CASCADE to all items (folders/notes/files) using stored procedure
        /// UNIFORMLY treats all tables: either ALL soft delete OR ALL hard delete
        /// </summary>
        public async Task<ResultOptions> DeleteWorkspacesCascadeAsync(string workspaceIds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(workspaceIds))
                {
                    _logger.LogWarning("Empty workspace IDs provided for cascade deletion");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No workspace IDs provided",
                        Status = 400
                    };
                }

                _logger.LogInformation("Cascade deleting workspaces with IDs: {WorkspaceIds})", 
                    workspaceIds);

                // Execute stored procedure
                var workspaceIdsParam = new SqlParameter("@iv_workspace_ids", SqlDbType.NVarChar, -1) { Value = workspaceIds };
                var deletedCountParam = new SqlParameter("@ov_deleted_count", SqlDbType.Int) { Direction = ParameterDirection.Output };

                // Execute stored procedure
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [ws].[sp_DeleteWorkspace] @iv_workspace_ids, @ov_deleted_count OUTPUT",
                    workspaceIdsParam, deletedCountParam
                );

                var deletedCount = (int)deletedCountParam.Value;

                if (deletedCount == 0)
                {
                    _logger.LogWarning("No workspaces found to delete with IDs: {WorkspaceIds}", workspaceIds);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"No workspaces found with IDs: {workspaceIds}",
                        Status = 404
                    };
                }

                _logger.LogInformation("Successfully cascade deleted {Count} workspaces)", 
                    deletedCount);
                    
                return new ResultOptions
                {
                    Success = true,
                    Message = $"Successfully permanently deleted {deletedCount} workspace(s) and all related items",
                    Reference = deletedCount.ToString(),
                    Status = 200
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while cascade deleting workspaces with IDs: {WorkspaceIds}", workspaceIds);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Database error: {ex.Message}",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cascade deleting workspaces with IDs: {WorkspaceIds}", workspaceIds);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Restores multiple deleted workspaces by IDs in a single batch operation (undo soft delete)
        /// </summary>
        public async Task<ResultOptions> UndoDeleteWorkspacesBatchAsync(List<int> workspaceIds)
        {
            try
            {
                if (workspaceIds == null || !workspaceIds.Any())
                {
                    _logger.LogWarning("Empty workspace IDs provided for batch undo deletion");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No workspace IDs provided",
                        Status = 400
                    };
                }

                _logger.LogInformation("Batch restoring workspaces with IDs: {WorkspaceIds}", string.Join(",", workspaceIds));

                // Update all matching workspaces in one query, ignore global filters to find deleted workspaces
                var affectedRows = await _context.Workspaces
                    .IgnoreQueryFilters()
                    .Where(w => workspaceIds.Contains(w.Id) && w.DeletedAt != null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(w => w.DeletedAt, (DateTime?)null)
                        .SetProperty(w => w.UpdatedAt, DateTime.UtcNow));

                if (affectedRows == 0)
                {
                    _logger.LogWarning("No deleted workspaces found with IDs: {WorkspaceIds}", string.Join(",", workspaceIds));
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"No deleted workspaces found with IDs: {string.Join(",", workspaceIds)}",
                        Status = 404
                    };
                }

                _logger.LogInformation("Successfully batch restored {Count} workspaces", affectedRows);
                return new ResultOptions
                {
                    Success = true,
                    Message = $"Successfully restored {affectedRows} workspace(s)",
                    Status = 200
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while batch restoring workspaces with IDs: {WorkspaceIds}", string.Join(",", workspaceIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while batch restoring workspaces",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch restoring workspaces with IDs: {WorkspaceIds}", string.Join(",", workspaceIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }
    }
}
