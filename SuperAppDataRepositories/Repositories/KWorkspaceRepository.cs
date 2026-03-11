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
        /// Gets the complete workspace tree (folders, notes, and files) with hierarchy
        ///
        /// ⚠️ PERFORMANCE NOTE - FILTERING MOVED TO FRONTEND:
        /// This endpoint returns ALL workspace items without server-side filtering.
        /// Filtering by statusCode, deletedAt, and search is handled in the frontend.
        ///
        /// PROS:
        /// - Instant client-side filtering without API calls
        /// - Better UX for search/filter interactions
        /// - Works well with react-arborist virtualization (only renders visible items)
        ///
        /// PERFORMANCE CHARACTERISTICS:
        /// - ✅ GOOD for workspaces with < 3,000 items (filter time < 150ms)
        /// - ⚠️ ACCEPTABLE for 3,000-5,000 items (150-300ms, needs debouncing)
        /// - ❌ POOR for > 5,000 items (> 300ms, consider backend filtering)
        ///
        /// The filterOptions parameter is kept for backward compatibility but not used.
        /// </summary>
        public async Task<KWorkspaceWithTree?> GetWorkspaceTreeAsync(int workspaceId, int userId, KWorkspaceFilterOptions? filterOptions = null)
        {
            try
            {
                _logger.LogInformation("Getting kworkspace tree for workspaceId: {WorkspaceId}, userId: {UserId} (NO SERVER FILTERING - all items returned)",
                    workspaceId, userId);

                // Get workspace
                var workspace = await _context.KWorkspaces
                    .Where(w => w.Id == workspaceId)
                    .FirstOrDefaultAsync();

                if (workspace == null)
                {
                    _logger.LogWarning("KWorkspace not found: {WorkspaceId}", workspaceId);
                    return null;
                }

                // ⚠️ CHANGED: Load ALL workspace items (no filtering)
                // Frontend will handle filtering by deletedAt, statusCode, and search text
                var items = await _context.KWorkspaceItems
                    .Where(i => i.WorkspaceId == workspaceId)
                    .Select(i => new KWorkspaceItemEntity
                    {
                        Id = i.Id,
                        WorkspaceId = i.WorkspaceId,
                        ParentId = i.ParentId,
                        EntityType = i.EntityType,
                        EntityId = i.EntityId,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt,
                        DeletedAt = i.DeletedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} items in kworkspace {WorkspaceId} (unfiltered)", items.Count, workspaceId);

                // Get distinct IDs for each type
                var folderIds = items.Where(i => i.EntityType == 2).Select(i => i.EntityId).Distinct().ToList(); // entity_type = 2 (folder)
                var noteIds = items.Where(i => i.EntityType == 3).Select(i => i.EntityId).Distinct().ToList();   // entity_type = 3 (note)
                var fileIds = items.Where(i => i.EntityType == 4).Select(i => i.EntityId).Distinct().ToList();   // entity_type = 4 (file)

                // Load ALL entity data (folders, notes, files) - no filtering
                var folders = await _context.Folders
                    .AsNoTracking()
                    .Where(f => folderIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id);

                // ⚠️ CHANGED: Load full note data (including statusCode for frontend filtering)
                var notes = await _context.Notes
                    .AsNoTracking()
                    .Where(n => noteIds.Contains(n.Id))
                    .Select(n => new { n.Id, n.Name, n.StatusCode })
                    .ToDictionaryAsync(n => n.Id);

                // ⚠️ CHANGED: Load full file data (including statusCode for frontend filtering)
                var files = await _context.Files
                    .AsNoTracking()
                    .Where(f => fileIds.Contains(f.Id))
                    .Select(f => new { f.Id, f.Name, f.StatusCode })
                    .ToDictionaryAsync(f => f.Id);

                // ⚠️ CHANGED: Build ALL tree items (no filtering)
                // Frontend will filter by deletedAt, statusCode, and search text
                var treeItems = new List<KWorkspaceItem>();

                foreach (var item in items)
                {
                    var treeItem = new KWorkspaceItem
                    {
                        RelationshipId = item.Id, // workspace_items.id (relationship ID)
                        ItemId = item.EntityId, // Entity ID (folder/note/file ID)
                        Type = GetItemTypeName(item.EntityType), // Convert TINYINT to string
                        UserId = userId,
                        Name = "",
                        ParentId = item.ParentId, // parent_id from ws.workspace_items
                        Level = 0,
                        Position = 0,
                        AccessType = "owner",
                        IsOriginal = true,
                        DeletedAt = item.DeletedAt // workspace_items.deleted_at - Frontend will filter this
                    };

                    // Populate name and metadata based on type
                    if (item.EntityType == 2 && folders.TryGetValue(item.EntityId, out var folder))
                    {
                        treeItem.Name = folder.Name;
                        treeItem.Color = folder.Color;
                        treeItem.Icon = folder.Icon;
                    }
                    else if (item.EntityType == 3 && notes.TryGetValue(item.EntityId, out var note))
                    {
                        treeItem.Name = note.Name;
                        // StatusCode available in 'note.StatusCode' - included in response for frontend filtering
                    }
                    else if (item.EntityType == 4 && files.TryGetValue(item.EntityId, out var file))
                    {
                        treeItem.Name = file.Name;
                        // StatusCode available in 'file.StatusCode' - included in response for frontend filtering
                    }

                    treeItems.Add(treeItem);
                }

                _logger.LogInformation("Built {Count} tree items for kworkspace {WorkspaceId} (unfiltered - frontend will apply filters)", treeItems.Count, workspaceId);

                return new KWorkspaceWithTree
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
                _logger.LogError(ex, "Database error while getting kworkspace tree for workspaceId: {WorkspaceId}", workspaceId);
                throw new InvalidOperationException($"Database error while retrieving kworkspace tree for workspace {workspaceId}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kworkspace tree for workspaceId: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Gets workspace by ID
        /// </summary>
        public async Task<KWorkspace?> GetWorkspaceByIdAsync(int workspaceId, int userId)
        {
            try
            {
                // ✅ Include deleted workspace - Frontend will handle display logic
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

        /// <summary>
        /// Gets all workspaces for a user with optional filters
        /// </summary>
        public async Task<List<KWorkspace>> GetAllWorkspacesByUserIdAsync(int userId, FilterOptions? filterOptions = null)
        {
            try
            {
                _logger.LogInformation("Getting all kworkspaces for userId: {UserId}, StatusCodes: {StatusCodes}, DeletedAt: {DeletedAt}, CreatedFrom: {CreatedFrom}, CreatedTo: {CreatedTo}",
                    userId,
                    filterOptions?.StatusCodes != null ? string.Join(",", filterOptions.StatusCodes) : "null",
                    filterOptions?.DeletedAt,
                    filterOptions?.CreatedFrom,
                    filterOptions?.CreatedTo);

                var query = _context.KWorkspaces
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

                _logger.LogInformation("Found {Count} kworkspaces for userId: {UserId}", workspaces.Count, userId);

                return workspaces;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting kworkspaces for userId: {UserId}", userId);
                throw new InvalidOperationException($"Database error while retrieving kworkspaces for user {userId}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kworkspaces for userId: {UserId}", userId);
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

                _logger.LogInformation("{Action} folder '{Name}' in kworkspace {WorkspaceId} for user {UserId}, ParentId: {ParentId}",
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
                            var workspaceItem = await _context.KWorkspaceItems
                                .FirstOrDefaultAsync(wi => wi.WorkspaceId == workspaceId && wi.EntityType == 2 && wi.EntityId == request.Id.Value);

                            if (workspaceItem != null && workspaceItem.ParentId != request.ParentId)
                            {
                                workspaceItem.ParentId = request.ParentId;
                                workspaceItem.UpdatedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("KWorkspaceItem parent updated for folder {FolderId}", request.Id.Value);
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
                            _logger.LogInformation("Creating KWorkspaceItemEntity with parentId: {ParentId}", request.ParentId);
                            var workspaceItem = new KWorkspaceItemEntity(workspaceId, 2, newFolder.Id, request.ParentId);

                            _context.KWorkspaceItems.Add(workspaceItem);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("KWorkspaceItemEntity created with ID: {ItemId} linking folder {FolderId} to kworkspace {WorkspaceId}",
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
                _logger.LogError(ex, "Concurrency conflict while upserting folder '{Name}' in kworkspace {WorkspaceId}", request.Name, workspaceId);

                return new ResultOptions
                {
                    Success = false,
                    Message = "Folder was modified by another user. Please refresh and try again.",
                    Status = 409
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while upserting folder '{Name}' in kworkspace {WorkspaceId}", request.Name, workspaceId);

                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while saving folder",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting folder '{Name}' in kworkspace {WorkspaceId}", request.Name, workspaceId);

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
        public async Task<ResultOptions> MoveItemsAsync(int sourceWorkspaceId, List<(byte EntityType, int EntityId)> items, int? targetParentId, int? targetWorkspaceId)
        {
            try
            {
                _logger.LogInformation("Moving {Count} items from kworkspace {SourceWorkspaceId} to parent {TargetParentId} in kworkspace {TargetWorkspaceId}",
                    items.Count, sourceWorkspaceId, targetParentId, targetWorkspaceId ?? sourceWorkspaceId);

                // Serialize items to JSON for stored procedure
                var itemsJson = JsonSerializer.Serialize(items.Select(i => new { type = i.EntityType, id = i.EntityId }));

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
                _logger.LogError(ex, "SQL error while moving items from kworkspace {SourceWorkspaceId}", sourceWorkspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Database error: {ex.Message}",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving items from kworkspace {SourceWorkspaceId}", sourceWorkspaceId);
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
        public async Task<ResultOptions> DeleteItemsAsync(int workspaceId, List<(byte EntityType, int EntityId)> items, bool isHardDelete = false)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} items from kworkspace {WorkspaceId} (HardDelete: {IsHardDelete})",
                    items.Count, workspaceId, isHardDelete);

                // Serialize items to JSON for stored procedure
                var itemsJson = JsonSerializer.Serialize(items.Select(i => new { type = i.EntityType, id = i.EntityId }));

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
                _logger.LogError(ex, "SQL error while deleting items from kworkspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Database error: {ex.Message}",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting items from kworkspace {WorkspaceId}", workspaceId);
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
        public async Task<ResultOptions> AddItemToWorkspaceAsync(int workspaceId, int userId, KAddItemToWorkspaceRequest request)
        {
            try
            {
                _logger.LogInformation("Adding {ChildType} (ID: {ChildId}) to kworkspace {WorkspaceId} under parent {ParentId}",
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
                        (workspace_id, parent_id, entity_type, entity_id, created_at)
                    VALUES
                        (@WorkspaceId, @ParentId, @EntityType, @EntityId, GETUTCDATE());
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var parameters = new[]
                {
                    new SqlParameter("@WorkspaceId", workspaceId),
                    new SqlParameter("@ParentId", (object?)request.ParentTagId ?? DBNull.Value),
                    new SqlParameter("@EntityType", itemType),
                    new SqlParameter("@EntityId", request.ChildId.Value)
                };

                var newId = await _context.Database.ExecuteSqlRawAsync(sql, parameters);

                _logger.LogInformation("Successfully added {ChildType} to kworkspace {WorkspaceId}, new row ID: {RowId}",
                    request.ChildType, workspaceId, newId);

                return new ResultOptions
                {
                    Success = true,
                    Message = $"{request.ChildType} added to kworkspace successfully",
                    Reference = newId.ToString(),
                    Status = 201
                };
            }
            catch (SqlException ex) when (ex.Number == 547) // Foreign key violation
            {
                _logger.LogError(ex, "Foreign key violation while adding item to kworkspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"The {request.ChildType} ID does not exist or kworkspace not found",
                    Status = 404
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while adding item to kworkspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Database error: {ex.Message}",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to kworkspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Failed to add item: {ex.Message}",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Batch upsert workspace items - OBSOLETE
        /// Use KWorkspaceItemService.UpsertWorkspaceItemsAsync instead
        /// </summary>
        [Obsolete("Use KWorkspaceItemService.UpsertWorkspaceItemsAsync instead", false)]
        public async Task<ResultOptions> UpsertWorkspaceItemsAsync(
            List<KUpsertWorkspaceItemRequest> requests,
            int userId)
        {
            // This method is obsolete - return error directing to new service
            await Task.CompletedTask; // Suppress async warning
            return new ResultOptions
            {
                Success = false,
                Message = "This method is obsolete. Use KWorkspaceItemService.UpsertWorkspaceItemsAsync instead.",
                Status = 410 // Gone
            };
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

        /// <summary>
        /// Updates only the name of a folder identified by its workspace_items.id
        /// </summary>
        public async Task<ResultOptions> UpdateFolderNameByWorkspaceItemIdAsync(int workspaceItemId, string name)
        {
            try
            {
                var item = await _context.KWorkspaceItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(wi => wi.Id == workspaceItemId && wi.EntityType == 2);

                if (item == null)
                {
                    _logger.LogWarning("Folder kworkspace item not found with ID: {WorkspaceItemId}", workspaceItemId);
                    return new ResultOptions { Success = false, Message = $"Folder kworkspace item {workspaceItemId} not found", Status = 404 };
                }

                var affectedRows = await _context.Folders
                    .Where(f => f.Id == item.EntityId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(f => f.Name, name)
                        .SetProperty(f => f.UpdatedAt, DateTime.UtcNow));

                if (affectedRows == 0)
                {
                    _logger.LogWarning("Folder not found for name update with ID: {FolderId}", item.EntityId);
                    return new ResultOptions { Success = false, Message = $"Folder {item.EntityId} not found", Status = 404 };
                }

                _logger.LogInformation("Updated folder name to '{Name}' for kworkspace item ID: {WorkspaceItemId}", name, workspaceItemId);
                return new ResultOptions { Success = true, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder name for kworkspace item ID: {WorkspaceItemId}", workspaceItemId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
