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

                // Get workspace - ✅ Include deleted workspaces
                var workspace = await _context.Workspaces
                    .Where(w => w.Id == workspaceId)
                    .FirstOrDefaultAsync();
                    // Frontend will handle display logic

                if (workspace == null)
                {
                    _logger.LogWarning("Workspace not found: {WorkspaceId}", workspaceId);
                    return null;
                }

                // Get all workspace items - ✅ Include deleted items
                // Frontend will handle display logic
                var items = await _context.WorkspaceItems
                    .Where(i => i.WorkspaceId == workspaceId)
                    .Select(i => new WorkspaceItemEntity
                    {
                        Id = i.Id,
                        WorkspaceId = i.WorkspaceId,
                        ParentId = i.ParentId,
                        ItemType = i.ItemType,
                        ItemId = i.ItemId,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt,
                        DeletedAt = i.DeletedAt,
                        CopyInfo = i.CopyInfo
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} items in workspace {WorkspaceId}", items.Count, workspaceId);

                // Get distinct IDs for each type
                var folderIds = items.Where(i => i.ItemType == 2).Select(i => i.ItemId).Distinct().ToList(); // item_type = 2 (folder)
                var noteIds = items.Where(i => i.ItemType == 3).Select(i => i.ItemId).Distinct().ToList();   // item_type = 3 (note)
                var fileIds = items.Where(i => i.ItemType == 4).Select(i => i.ItemId).Distinct().ToList();   // item_type = 4 (file)

                // ✅ Include deleted folders - Frontend will handle display logic
                var folders = await _context.Folders
                    .AsNoTracking()
                    .Where(f => folderIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id);

                // ✅ Include deleted notes - Frontend will handle display logic
                var notes = await _context.Notes
                    .AsNoTracking()
                    .Where(n => noteIds.Contains(n.Id))
                    .ToDictionaryAsync(n => n.Id);

                // ✅ Include deleted files - Frontend will handle display logic
                var files = await _context.Files
                    .AsNoTracking()
                    .Where(f => fileIds.Contains(f.Id))
                    .Select(f => new SuperAppModels.Models.File 
                    { 
                        Id = f.Id, 
                        Name = f.Name,
                        DeletedAt = f.DeletedAt // ✅ Include DeletedAt
                    })
                    .ToDictionaryAsync(f => f.Id);

                // Build tree items
                var treeItems = new List<WorkspaceItem>();

                foreach (var item in items)
                {
                    var treeItem = new WorkspaceItem
                    {
                        RelationshipId = item.Id, // workspace_items.id (relationship ID)
                        ItemId = item.ItemId, // Entity ID (folder/note/file ID)
                        Type = GetItemTypeName(item.ItemType), // Convert TINYINT to string
                        UserId = userId,
                        Name = "",
                        ParentId = item.ParentId, // parent_id from ws.workspace_items
                        Level = 0,
                        Position = 0,
                        AccessType = "owner", // Simplified: always owner for now
                        IsOriginal = true, // Simplified: always true for now
                        DeletedAt = item.DeletedAt // ✅ Include DeletedAt from workspace_items
                    };

                    // Populate name, metadata, and DeletedAt based on type
                    if (item.ItemType == 2 && folders.TryGetValue(item.ItemId, out var folder))
                    {
                        treeItem.Name = folder.Name;
                        treeItem.Color = folder.Color;
                        treeItem.Icon = folder.Icon;
                        // ✅ Use entity DeletedAt if relationship is not deleted but entity is
                        if (treeItem.DeletedAt == null && folder.DeletedAt != null)
                        {
                            treeItem.DeletedAt = folder.DeletedAt;
                        }
                    }
                    else if (item.ItemType == 3 && notes.TryGetValue(item.ItemId, out var note))
                    {
                        treeItem.Name = note.Name;
                        // ✅ Use entity DeletedAt if relationship is not deleted but entity is
                        if (treeItem.DeletedAt == null && note.DeletedAt != null)
                        {
                            treeItem.DeletedAt = note.DeletedAt;
                        }
                    }
                    else if (item.ItemType == 4 && files.TryGetValue(item.ItemId, out var file))
                    {
                        treeItem.Name = file.Name;
                        // ✅ Use entity DeletedAt if relationship is not deleted but entity is
                        if (treeItem.DeletedAt == null && file.DeletedAt != null)
                        {
                            treeItem.DeletedAt = file.DeletedAt;
                        }
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
                // ✅ Include deleted workspace - Frontend will handle display logic
                return await _context.Workspaces
                    .Where(w => w.Id == workspaceId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace by ID: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Gets all workspaces for a user with optional filters
        /// </summary>
        public async Task<List<Workspace>> GetAllWorkspacesByUserIdAsync(int userId, FilterOptions? filterOptions = null)
        {
            try
            {
                _logger.LogInformation("Getting all workspaces for userId: {UserId}, StatusCodes: {StatusCodes}, DeletedAt: {DeletedAt}, CreatedFrom: {CreatedFrom}, CreatedTo: {CreatedTo}",
                    userId,
                    filterOptions?.StatusCodes != null ? string.Join(",", filterOptions.StatusCodes) : "null",
                    filterOptions?.DeletedAt,
                    filterOptions?.CreatedFrom,
                    filterOptions?.CreatedTo);

                var query = _context.Workspaces
                    .Where(w => w.UserId == userId);

                // Apply filters if provided
                if (filterOptions != null)
                {
                    // Filter by status code
                    if (filterOptions.StatusCodes != null && filterOptions.StatusCodes.Any())
                    {
                        query = query.Where(w => w.StatusCode != null && filterOptions.StatusCodes.Contains(w.StatusCode));
                    }

                    // Filter by deletedAt
                    if (!string.IsNullOrEmpty(filterOptions.DeletedAt))
                    {
                        if (filterOptions.DeletedAt == "null")
                        {
                            query = query.Where(w => w.DeletedAt == null);
                        }
                        else if (filterOptions.DeletedAt == "notNull")
                        {
                            query = query.Where(w => w.DeletedAt != null);
                        }
                    }

                    // Filter by created date range
                    if (filterOptions.CreatedFrom.HasValue)
                    {
                        query = query.Where(w => w.CreatedAt >= filterOptions.CreatedFrom.Value);
                    }

                    if (filterOptions.CreatedTo.HasValue)
                    {
                        query = query.Where(w => w.CreatedAt <= filterOptions.CreatedTo.Value);
                    }

                    // Filter by search text
                    if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                    {
                        query = query.Where(w => w.Name.Contains(filterOptions.SearchText) ||
                                               (w.Description != null && w.Description.Contains(filterOptions.SearchText)));
                    }
                }

                var workspaces = await query
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
                            var workspaceItem = new WorkspaceItemEntity(workspaceId, 2, newFolder.Id, request.ParentId);

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
                var itemsJson = JsonSerializer.Serialize(items.Select(i => new { type = i.ItemType, id = i.ItemId }));

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
        public async Task<ResultOptions> DeleteItemsAsync(int workspaceId, List<(byte ItemType, int ItemId)> items, bool isHardDelete = false)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} items from workspace {WorkspaceId} (HardDelete: {IsHardDelete})",
                    items.Count, workspaceId, isHardDelete);

                // Serialize items to JSON for stored procedure
                var itemsJson = JsonSerializer.Serialize(items.Select(i => new { type = i.ItemType, id = i.ItemId }));

                // Execute stored procedure with new parameter names
                var workspaceIdParam = new SqlParameter("@iv_workspace_id", workspaceId);
                var itemsParam = new SqlParameter("@iv_items", SqlDbType.NVarChar, -1) { Value = itemsJson };
                var isHardDeleteParam = new SqlParameter("@iv_is_hardDelete", isHardDelete);
                var deletedCountParam = new SqlParameter("@ov_deleted_count", SqlDbType.Int) { Direction = ParameterDirection.Output };

                // Execute and get result
                var result = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [ws].[sp_DeleteWorkspaceItems] @iv_workspace_id, @iv_items, @iv_is_hardDelete, @ov_deleted_count OUTPUT",
                    workspaceIdParam, itemsParam, isHardDeleteParam, deletedCountParam
                );

                var deletedCount = (int)deletedCountParam.Value;
                _logger.LogInformation("Successfully deleted {Count} items (including children)", deletedCount);

                return new ResultOptions
                {
                    Success = true,
                    Message = isHardDelete ? "Items permanently deleted" : "Items deleted successfully",
                    Reference = deletedCount.ToString(),
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
        /// Adds an item (folder/note/file) to a workspace
        /// Creates a new row in workspace_items table
        /// </summary>
        public async Task<ResultOptions> AddItemToWorkspaceAsync(int workspaceId, int userId, AddItemToWorkspaceRequest request)
        {
            try
            {
                _logger.LogInformation("Adding {ChildType} (ID: {ChildId}) to workspace {WorkspaceId} under parent {ParentId}",
                    request.ChildType, request.ChildId, workspaceId, request.ParentTagId);

                // Map child type string to TINYINT
                byte itemType = request.ChildType.ToLower() switch
                {
                    "folder" or "tag" => 2,
                    "note" => 3,
                    "file" => 4,
                    _ => throw new ArgumentException($"Invalid child type: {request.ChildType}")
                };

                // Validate child ID
                if (!request.ChildId.HasValue || request.ChildId.Value <= 0)
                {
                    _logger.LogWarning("Invalid child ID: {ChildId}", request.ChildId);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Child ID is required and must be positive",
                        Status = 400
                    };
                }

                // Insert into workspace_items
                var sql = @"
                    INSERT INTO [ws].[workspace_items]
                        (workspace_id, parent_id, item_type, item_id, created_at)
                    VALUES
                        (@WorkspaceId, @ParentId, @ItemType, @ItemId, GETUTCDATE());
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var parameters = new[]
                {
                    new SqlParameter("@WorkspaceId", workspaceId),
                    new SqlParameter("@ParentId", (object?)request.ParentTagId ?? DBNull.Value),
                    new SqlParameter("@ItemType", itemType),
                    new SqlParameter("@ItemId", request.ChildId.Value)
                };

                var newId = await _context.Database.ExecuteSqlRawAsync(sql, parameters);

                _logger.LogInformation("Successfully added {ChildType} to workspace {WorkspaceId}, new row ID: {RowId}",
                    request.ChildType, workspaceId, newId);

                return new ResultOptions
                {
                    Success = true,
                    Message = $"{request.ChildType} added to workspace successfully",
                    Reference = newId.ToString(),
                    Status = 201
                };
            }
            catch (SqlException ex) when (ex.Number == 547) // Foreign key violation
            {
                _logger.LogError(ex, "Foreign key violation while adding item to workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"The {request.ChildType} ID does not exist or workspace not found",
                    Status = 404
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while adding item to workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Database error: {ex.Message}",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Failed to add item: {ex.Message}",
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
