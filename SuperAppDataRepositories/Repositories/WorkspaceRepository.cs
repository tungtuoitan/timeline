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
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkspaceRepository> _logger;

        public WorkspaceRepository(
            ApplicationDbContext context,
            ILogger<WorkspaceRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the complete workspace tree (folders, notes, and files) with hierarchy
        /// </summary>
        public async Task<WorkspaceWithTree?> GetWorkspaceTreeAsync(int workspaceId, int userId)
        {
            try
            {
                _logger.LogInformation("Getting workspace tree for workspaceId: {WorkspaceId}, userId: {UserId}",
                    workspaceId, userId);

                // Get workspace
                var workspace = await _context.Workspaces
                    .Where(w => w.Id == workspaceId && w.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (workspace == null)
                {
                    _logger.LogWarning("Workspace not found: {WorkspaceId}", workspaceId);
                    return null;
                }

                // Get all workspace items - Project to avoid navigation loading
                var items = await _context.WorkspaceItems
                    .Where(i => i.WorkspaceId == workspaceId)
                    .Select(i => new WorkspaceItemEntity
                    {
                        Id = i.Id,
                        WorkspaceId = i.WorkspaceId,
                        ParentId = i.ParentId,
                        ItemType = i.ItemType,
                        ItemId = i.ItemId,
                        IsOriginal = i.IsOriginal,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt,
                        DeletedAt = i.DeletedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} items in workspace {WorkspaceId}", items.Count, workspaceId);

                // Get distinct IDs for each type
                var folderIds = items.Where(i => i.ItemType == 2).Select(i => i.ItemId).Distinct().ToList(); // item_type = 2 (folder)
                var noteIds = items.Where(i => i.ItemType == 3).Select(i => i.ItemId).Distinct().ToList();   // item_type = 3 (note)
                var fileIds = items.Where(i => i.ItemType == 4).Select(i => i.ItemId).Distinct().ToList();   // item_type = 4 (file)

                var folders = await _context.Folders
                    .AsNoTracking()
                    .Where(f => folderIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id);

                var notes = await _context.Notes
                    .AsNoTracking()
                    .Where(n => noteIds.Contains(n.Id))
                    .ToDictionaryAsync(n => n.Id);

                var files = await _context.Files
                    .AsNoTracking()
                    .Where(f => fileIds.Contains(f.Id))
                    .Select(f => new SuperAppModels.Models.File { Id = f.Id, Name = f.Name })
                    .ToDictionaryAsync(f => f.Id);

                // Build tree items
                var treeItems = new List<WorkspaceItem>();

                foreach (var item in items)
                {
                    var treeItem = new WorkspaceItem
                    {
                        ItemId = item.ItemId,
                        ItemType = GetItemTypeName(item.ItemType), // Convert TINYINT to string
                        UserId = userId,
                        Name = "",
                        ParentId = item.ParentId, // parent_id from ws.workspace_items
                        Level = 0,
                        Position = 0,
                        AccessType = item.IsOriginal ? "owner" : "shared",
                        IsOriginal = item.IsOriginal
                    };

                    // Populate name and metadata based on type
                    if (item.ItemType == 2 && folders.TryGetValue(item.ItemId, out var folder))
                    {
                        treeItem.Name = folder.Name;
                        treeItem.Color = folder.Color;
                        treeItem.Icon = folder.Icon;
                    }
                    else if (item.ItemType == 3 && notes.TryGetValue(item.ItemId, out var note))
                    {
                        treeItem.Name = note.Name;
                    }
                    else if (item.ItemType == 4 && files.TryGetValue(item.ItemId, out var file))
                    {
                        treeItem.Name = file.Name;
                    }

                    treeItems.Add(treeItem);
                }

                _logger.LogInformation("Built {Count} tree items for workspace {WorkspaceId}", treeItems.Count, workspaceId);

                return new WorkspaceWithTree
                {
                    WorkspaceId = workspace.Id,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    UserId = workspace.UserId,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    Items = treeItems
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting workspace tree for workspaceId: {WorkspaceId}", workspaceId);
                throw new InvalidOperationException($"Database error while retrieving workspace tree for workspace {workspaceId}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace tree for workspaceId: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Gets workspace by ID
        /// </summary>
        public async Task<Workspace?> GetWorkspaceByIdAsync(int workspaceId, int userId)
        {
            try
            {
                return await _context.Workspaces
                    .Where(w => w.Id == workspaceId && w.DeletedAt == null)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace by ID: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Gets all workspaces for a user
        /// </summary>
        public async Task<List<Workspace>> GetAllWorkspacesByUserIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting all workspaces for userId: {UserId}", userId);

                var workspaces = await _context.Workspaces
                    .Where(w => w.UserId == userId && w.DeletedAt == null)
                    .OrderBy(w => w.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} workspaces for userId: {UserId}", workspaces.Count, userId);

                return workspaces;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting workspaces for userId: {UserId}", userId);
                throw new InvalidOperationException($"Database error while retrieving workspaces for user {userId}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspaces for userId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Creates or updates a folder in a workspace
        /// </summary>
        public async Task<ResultOptions> UpsertFolderAsync(int workspaceId, int userId, UpsertFolderRequest request)
        {
            try
            {
                var isUpdate = request.Id.HasValue;
                var action = isUpdate ? "Updating" : "Creating";
                
                _logger.LogInformation("{Action} folder '{Name}' in workspace {WorkspaceId} for user {UserId}, ParentId: {ParentId}",
                    action, request.Name, workspaceId, userId, request.ParentId);

                // Use execution strategy to handle retries with transactions
                var strategy = _context.Database.CreateExecutionStrategy();

                var folder = await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        Folder resultFolder;

                        if (isUpdate)
                        {
                            // Update existing folder
                            var existingFolder = await _context.Folders
                                .FirstOrDefaultAsync(f => f.Id == request.Id.Value && f.UserId == userId);

                            if (existingFolder == null)
                            {
                                throw new Exception($"Folder with ID {request.Id} not found or access denied");
                            }

                            existingFolder.Name = request.Name;
                            existingFolder.Description = request.Description;
                            existingFolder.Color = request.Color ?? existingFolder.Color;
                            existingFolder.Icon = request.Icon ?? existingFolder.Icon;
                            existingFolder.UpdatedAt = DateTime.UtcNow;

                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Folder updated with ID: {FolderId}", existingFolder.Id);

                            // Update parent folder if changed
                            var workspaceItem = await _context.WorkspaceItems
                                .FirstOrDefaultAsync(wi => wi.WorkspaceId == workspaceId && wi.ItemType == 2 && wi.ItemId == request.Id.Value);

                            if (workspaceItem != null && workspaceItem.ParentId != request.ParentId)
                            {
                                workspaceItem.ParentId = request.ParentId;
                                workspaceItem.UpdatedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("WorkspaceItem parent updated for folder {FolderId}", request.Id.Value);
                            }

                            resultFolder = existingFolder;
                        }
                        else
                        {
                            // Create new folder
                            var newFolder = new Folder(request.Name, userId)
                            {
                                Description = request.Description,
                                Color = request.Color ?? "#F59E0B",  // Default amber color
                                Icon = request.Icon ?? "📁"           // Default folder emoji
                            };

                            _context.Folders.Add(newFolder);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("Folder created with ID: {FolderId}", newFolder.Id);

                            // Create workspace_item link (ws.workspace_items table)
                            _logger.LogInformation("Creating WorkspaceItemEntity with parentId: {ParentId}", request.ParentId);
                            var workspaceItem = new WorkspaceItemEntity(workspaceId, 2, newFolder.Id, request.ParentId)
                            {
                                IsOriginal = true  // This workspace owns the folder
                            };

                            _context.WorkspaceItems.Add(workspaceItem);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("WorkspaceItemEntity created with ID: {ItemId} linking folder {FolderId} to workspace {WorkspaceId}",
                                workspaceItem.Id, newFolder.Id, workspaceId);

                            resultFolder = newFolder;
                        }

                        await transaction.CommitAsync();
                        return resultFolder;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                var successMessage = isUpdate ? "Folder updated successfully" : "Folder created successfully";
                return new ResultOptions
                {
                    Success = true,
                    Message = successMessage,
                    Reference = folder.Id.ToString(),
                    Object = folder,
                    Status = isUpdate ? 200 : 201
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while upserting folder '{Name}' in workspace {WorkspaceId}", request.Name, workspaceId);
                
                return new ResultOptions
                {
                    Success = false,
                    Message = "Folder was modified by another user. Please refresh and try again.",
                    Status = 409
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while upserting folder '{Name}' in workspace {WorkspaceId}", request.Name, workspaceId);
                
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while saving folder",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting folder '{Name}' in workspace {WorkspaceId}", request.Name, workspaceId);
                
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Failed to upsert folder: {ex.Message}",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Moves multiple workspace items (folders/notes/files) with cascade support
        /// Uses stored procedure sp_MoveWorkspaceItems for recursive hierarchy handling
        /// </summary>
        public async Task<ResultOptions> MoveItemsAsync(int sourceWorkspaceId, List<(byte ItemType, int ItemId)> items, int? targetParentId, int? targetWorkspaceId)
        {
            try
            {
                _logger.LogInformation("Moving {Count} items from workspace {SourceWorkspaceId} to parent {TargetParentId} in workspace {TargetWorkspaceId}",
                    items.Count, sourceWorkspaceId, targetParentId, targetWorkspaceId ?? sourceWorkspaceId);

                // Serialize items to JSON for stored procedure
                var itemsJson = JsonSerializer.Serialize(items.Select(i => new { itemType = i.ItemType, itemId = i.ItemId }));

                // Execute stored procedure
                var sourceWorkspaceIdParam = new SqlParameter("@SourceWorkspaceId", sourceWorkspaceId);
                var itemsParam = new SqlParameter("@Items", SqlDbType.NVarChar, -1) { Value = itemsJson };
                var targetParentIdParam = new SqlParameter("@TargetParentId", SqlDbType.Int) { Value = (object?)targetParentId ?? DBNull.Value };
                var targetWorkspaceIdParam = new SqlParameter("@TargetWorkspaceId", SqlDbType.Int) { Value = (object?)targetWorkspaceId ?? DBNull.Value };

                // Execute and get result
                var result = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [ws].[sp_MoveWorkspaceItems] @SourceWorkspaceId, @Items, @TargetParentId, @TargetWorkspaceId",
                    sourceWorkspaceIdParam, itemsParam, targetParentIdParam, targetWorkspaceIdParam
                );

                _logger.LogInformation("Successfully moved {Count} items (including children)", result);

                return new ResultOptions
                {
                    Success = true,
                    Message = "Items moved successfully",
                    Reference = result.ToString(),
                    Status = 200
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while moving items from workspace {SourceWorkspaceId}", sourceWorkspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Database error: {ex.Message}",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving items from workspace {SourceWorkspaceId}", sourceWorkspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Failed to move items: {ex.Message}",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Deletes multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        public async Task<ResultOptions> DeleteItemsAsync(int workspaceId, List<(byte ItemType, int ItemId)> items)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} items from workspace {WorkspaceId}",
                    items.Count, workspaceId);

                // Serialize items to JSON for stored procedure
                var itemsJson = JsonSerializer.Serialize(items.Select(i => new { itemType = i.ItemType, itemId = i.ItemId }));

                // Execute stored procedure
                var workspaceIdParam = new SqlParameter("@WorkspaceId", workspaceId);
                var itemsParam = new SqlParameter("@Items", SqlDbType.NVarChar, -1) { Value = itemsJson };

                // Execute and get result
                var result = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [ws].[sp_DeleteWorkspaceItems] @WorkspaceId, @Items",
                    workspaceIdParam, itemsParam
                );

                _logger.LogInformation("Successfully deleted {Count} items (including children)", result);

                return new ResultOptions
                {
                    Success = true,
                    Message = "Items deleted successfully",
                    Reference = result.ToString(),
                    Status = 200
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while deleting items from workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Database error: {ex.Message}",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting items from workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Failed to delete items: {ex.Message}",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Helper method to convert TINYINT item_type to string name
        /// </summary>
        private string GetItemTypeName(byte itemType)
        {
            return itemType switch
            {
                1 => "workspace",
                2 => "folder",
                3 => "note",
                4 => "file",
                _ => "unknown"
            };
        }
    }
}
