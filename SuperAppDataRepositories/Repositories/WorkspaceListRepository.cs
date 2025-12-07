using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for workspace list data access (ws.workspaces table)
    /// </summary>
    public class WorkspaceListRepository : IWorkspaceListRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkspaceListRepository> _logger;

        public WorkspaceListRepository(
            ApplicationDbContext context,
            ILogger<WorkspaceListRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all workspaces with comprehensive filtering options
        /// </summary>
        public async Task<ResultOptions> GetWorkspacesAsync(WorkspaceFilterOptions filterOptions)
        {
            try
            {
                if (filterOptions == null)
                    throw new ArgumentNullException(nameof(filterOptions));

                _logger.LogInformation(
                    "Getting workspaces with filters - UserId: {UserId}, GetAll: {GetAll}, SearchText: {SearchText}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                    filterOptions.UserId, filterOptions.GetAll, 
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
        /// Creates a new workspace
        /// </summary>
        public async Task<ResultOptions> CreateWorkspaceAsync(Workspace workspace)
        {
            try
            {
                if (workspace == null)
                    throw new ArgumentNullException(nameof(workspace));

                _logger.LogInformation("Creating workspace with Name: '{Name}', UserId: {UserId}",
                    workspace.Name, workspace.UserId);

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
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while creating workspace");
                return new ResultOptions
                {
                    Success = false,
                    Message = "Concurrency conflict occurred while creating workspace",
                    Status = 500
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating workspace");
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while creating workspace",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workspace with Name: '{Name}'", workspace.Name);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Updates an existing workspace
        /// </summary>
        public async Task<ResultOptions> UpdateWorkspaceAsync(Workspace workspace)
        {
            try
            {
                if (workspace == null)
                    throw new ArgumentNullException(nameof(workspace));

                _logger.LogInformation("Updating workspace ID: {WorkspaceId}, Name: '{Name}'",
                    workspace.Id, workspace.Name);

                var existingWorkspace = await _context.Workspaces
                    .FirstOrDefaultAsync(w => w.Id == workspace.Id && w.DeletedAt == null);

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

                // Update properties
                existingWorkspace.Name = workspace.Name;
                existingWorkspace.Description = workspace.Description;
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
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while updating workspace ID: {WorkspaceId}", workspace.Id);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Workspace {workspace.Id} was modified by another user",
                    Status = 409
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating workspace ID: {WorkspaceId}", workspace.Id);
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while updating workspace",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workspace ID: {WorkspaceId}", workspace.Id);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Deletes multiple workspaces by IDs in a single batch operation (soft or hard delete)
        /// </summary>
        public async Task<ResultOptions> DeleteWorkspacesBatchAsync(List<int> workspaceIds, bool isHardDelete = false)
        {
            try
            {
                if (workspaceIds == null || !workspaceIds.Any())
                {
                    _logger.LogWarning("Empty workspace IDs provided for batch deletion");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No workspace IDs provided",
                        Status = 400
                    };
                }

                _logger.LogInformation("Batch deleting workspaces with IDs: {WorkspaceIds} (HardDelete: {IsHardDelete})", 
                    string.Join(",", workspaceIds), isHardDelete);

                int affectedRows;

                if (isHardDelete)
                {
                    // Hard delete - permanently remove from database
                    affectedRows = await _context.Workspaces
                        .Where(w => workspaceIds.Contains(w.Id))
                        .ExecuteDeleteAsync();
                }
                else
                {
                    // Soft delete - set deleted_at timestamp
                    affectedRows = await _context.Workspaces
                        .Where(w => workspaceIds.Contains(w.Id) && w.DeletedAt == null)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(w => w.DeletedAt, DateTime.UtcNow));
                }

                if (affectedRows == 0)
                {
                    _logger.LogWarning("No workspaces found to delete with IDs: {WorkspaceIds}", string.Join(",", workspaceIds));
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"No workspaces found with IDs: {string.Join(",", workspaceIds)}",
                        Status = 404
                    };
                }

                _logger.LogInformation("Successfully batch deleted {Count} workspaces (HardDelete: {IsHardDelete})", 
                    affectedRows, isHardDelete);
                    
                return new ResultOptions
                {
                    Success = true,
                    Message = $"Successfully deleted {affectedRows} workspace(s)",
                    Status = 200
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while batch deleting workspaces with IDs: {WorkspaceIds}", string.Join(",", workspaceIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while batch deleting workspaces",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch deleting workspaces with IDs: {WorkspaceIds}", string.Join(",", workspaceIds));
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
