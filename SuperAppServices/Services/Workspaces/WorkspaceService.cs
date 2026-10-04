using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;
using SuperAppDataRepositories.Data;

namespace SuperAppServices.Services.Workspaces
{
    /// <summary>
    /// Service for workspace operations
    /// </summary>
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<WorkspaceService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        // tree/v2 was measured at p95 ~13s with full prod data. Cache 60s sliding —
        // workspace trees change infrequently per user, so hit rate ~95% expected.
        private static readonly TimeSpan TreeV2CacheTtl = TimeSpan.FromSeconds(60);

        public WorkspaceService(
            IWorkspaceRepository workspaceRepository,
            IMapper mapper,
            ILogger<WorkspaceService> logger,
            ApplicationDbContext context,
            IMemoryCache cache)
        {
            _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        private static string TreeV2CacheKey(int workspaceId, int userId) =>
            $"tree-v2:{userId}:{workspaceId}";

        private void InvalidateTreeV2(int workspaceId, int userId)
        {
            _cache.Remove(TreeV2CacheKey(workspaceId, userId));
        }

        /// <summary>
        /// Gets all workspaces for a user with optional filters
        /// </summary>
        public async Task<List<WsResponse>> GetAllUserWorkspacesAsync(int userId, FilterOptions? filterOptions = null)
        {
            try
            {
                _logger.LogInformation("Getting all workspaces for UserId: {UserId}, StatusCodes: {StatusCodes}, DeletedAt: {DeletedAt}, CreatedFrom: {CreatedFrom}, CreatedTo: {CreatedTo}",
                    userId,
                    filterOptions?.StatusCodes != null ? string.Join(",", filterOptions.StatusCodes) : "null",
                    filterOptions?.DeletedAt,
                    filterOptions?.CreatedFrom,
                    filterOptions?.CreatedTo);

                var workspaces = await _workspaceRepository.GetAllWorkspacesByUserIdAsync(userId, filterOptions);

                var response = _mapper.Map<List<WsResponse>>(workspaces);

                _logger.LogInformation("Successfully retrieved {Count} workspaces for user {UserId}",
                    response.Count, userId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspaces for UserId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets workspace tree with all items (tags, notes, files)
        /// </summary>
        public async Task<WorkspaceWithTreeResponse> GetWorkspaceTreeAsync(int workspaceId, int userId)
        {
            try
            {
                _logger.LogInformation("Getting workspace tree for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);

                // Get workspace info
                var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, userId);
                if (workspace == null)
                {
                    _logger.LogWarning("Workspace {WorkspaceId} not found for user {UserId}",
                        workspaceId, userId);
                    throw new KeyNotFoundException($"Workspace with ID {workspaceId} not found");
                }

                // Get workspace tree (tags, notes, files)
                var workspaceWithTree = await _workspaceRepository.GetWorkspaceTreeAsync(workspaceId, userId);
                if (workspaceWithTree == null)
                {
                    _logger.LogWarning("Workspace tree data not found for workspace {WorkspaceId}", workspaceId);
                    throw new InvalidOperationException($"Failed to retrieve workspace tree for workspace {workspaceId}");
                }

                // Map to WorkspaceItemResponse using AutoMapper
                var flatResponse = _mapper.Map<List<WorkspaceItemResponse>>(workspaceWithTree.Items);

                // ✅ Return FLAT data - Frontend will build hierarchy when needed
                // This improves: payload size, caching, real-time updates, flexibility

                // Create response
                var response = new WorkspaceWithTreeResponse
                {
                    WorkspaceId = workspace.Id,
                    UserId = workspace.UserId,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    // Default values for properties not in DB schema
                    Color = "#3B82F6", // Default blue
                    Icon = "📁",
                    Type = "hierarchy",
                    MaxDepth = 10,
                    IsDefault = false,
                    IsPublic = false,
                    IsTemplate = false,
                    FolderCount = 0,
                    MemberCount = 1,
                    Settings = null,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    Items = flatResponse // ✅ FLAT list with parentId - Frontend builds hierarchy
                };

                _logger.LogInformation("Successfully retrieved workspace tree with {ItemCount} items for workspace {WorkspaceId}",
                    flatResponse.Count, workspaceId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspace tree for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                throw;
            }
        }

        /// <summary>
        /// Gets workspace tree V2 with ALL items (no server-side filtering)
        ///
        /// ⚠️ PERFORMANCE NOTE:
        /// This endpoint returns ALL workspace items. Frontend handles filtering by:
        /// - deletedAt (show/hide deleted items)
        /// - statusCode (draft/published notes/files)
        /// - search text (name matching)
        ///
        /// The filterOptions parameter is kept for backward compatibility but ignored.
        ///
        /// See WorkspaceRepository.GetWorkspaceTreeAsync for detailed performance notes.
        /// </summary>
        public async Task<WorkspaceDTO> GetWorkspaceTreeV2Async(int workspaceId, int userId, WorkspaceFilterOptions? filterOptions = null)
        {
            // filterOptions documented as ignored — safe to leave out of the cache key.
            var cacheKey = TreeV2CacheKey(workspaceId, userId);
            if (_cache.TryGetValue<WorkspaceDTO>(cacheKey, out var cached) && cached != null)
            {
                _logger.LogInformation("Tree V2 cache HIT for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                return cached;
            }

            try
            {
                _logger.LogInformation("Tree V2 cache MISS for WorkspaceId: {WorkspaceId}, UserId: {UserId} — building from DB",
                    workspaceId, userId);

                // ── Phase 1: workspace + items ──────────────────────────────
                // Filter by userId so callers can't read other users' workspaces.
                var workspace = await _context.Workspaces
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId && w.UserId == userId);

                if (workspace == null)
                {
                    _logger.LogWarning("Workspace {WorkspaceId} not found for user {UserId}",
                        workspaceId, userId);
                    throw new KeyNotFoundException($"Workspace with ID {workspaceId} not found");
                }

                var items = await _context.WorkspaceItems
                    .AsNoTracking()
                    .Where(i => i.WorkspaceId == workspaceId)
                    .ToListAsync();

                // ── Phase 2: folders/notes/files (sequential) ───────────────
                // Sequential because ApplicationDbContext is scoped and EF Core
                // forbids concurrent ops. Parallelizing would require IDbContextFactory.
                var folderIds = items.Where(i => i.EntityType == 2).Select(i => i.EntityId).Distinct().ToList();
                var noteIds   = items.Where(i => i.EntityType == 3).Select(i => i.EntityId).Distinct().ToList();
                var fileIds   = items.Where(i => i.EntityType == 4).Select(i => i.EntityId).Distinct().ToList();

                var foldersDict = folderIds.Count > 0
                    ? await LoadFoldersAsync(folderIds)
                    : new Dictionary<int, SuperAppModels.Models.Folder>();
                var notesDict = noteIds.Count > 0
                    ? await LoadNotesAsync(noteIds)
                    : new Dictionary<int, SuperAppModels.Models.Note>();
                var filesDict = fileIds.Count > 0
                    ? await LoadFilesAsync(fileIds)
                    : new Dictionary<int, SuperAppModels.Models.File>();

                // ── Phase 3: build response items in a single pass ──────────
                var itemsV2 = new List<WorkspaceItemResponseV2>(items.Count);
                foreach (var item in items)
                {
                    object entityData = item.EntityType switch
                    {
                        2 when foldersDict.TryGetValue(item.EntityId, out var f) => new FolderData
                        {
                            Id = f.Id,
                            UserId = f.UserId,
                            Name = f.Name,
                            Description = f.Description,
                            Color = f.Color,
                            Icon = f.Icon,
                            CreatedAt = f.CreatedAt ?? DateTime.UtcNow,
                            UpdatedAt = f.UpdatedAt,
                            DeletedAt = f.DeletedAt
                        },
                        3 when notesDict.TryGetValue(item.EntityId, out var n) => new NoteData
                        {
                            Id = n.Id,
                            UserId = n.UserId,
                            Name = n.Name,
                            Description = n.Description,
                            StatusCode = n.StatusCode,
                            Icon = n.Icon,
                            Color = n.Color,
                            CreatedAt = n.CreatedAt ?? DateTime.UtcNow,
                            UpdatedAt = n.UpdatedAt,
                            DeletedAt = n.DeletedAt
                        },
                        4 when filesDict.TryGetValue(item.EntityId, out var fl) => new FileData
                        {
                            Id = fl.Id,
                            UserId = fl.UserId,
                            Name = fl.Name,
                            Url = fl.Url,
                            FileSize = fl.FileSize,
                            MimeType = fl.MimeType,
                            Extension = fl.Extension,
                            StatusCode = fl.StatusCode,
                            CreatedAt = fl.CreatedAt ?? DateTime.UtcNow,
                            UpdatedAt = fl.UpdatedAt,
                            DeletedAt = fl.DeletedAt
                        },
                        // Fallback: entity hard-deleted but workspace_item row still references it.
                        2 => (object)new FolderData { Id = item.EntityId, Name = "[Deleted]", DeletedAt = item.DeletedAt },
                        3 => (object)new NoteData   { Id = item.EntityId, Name = "[Deleted]", DeletedAt = item.DeletedAt },
                        4 => (object)new FileData   { Id = item.EntityId, Name = "[Deleted]", DeletedAt = item.DeletedAt },
                        _ => throw new InvalidOperationException($"Unknown entity type: {item.EntityType}")
                    };

                    itemsV2.Add(new WorkspaceItemResponseV2
                    {
                        Id = item.Id,
                        WorkspaceId = item.WorkspaceId,
                        ParentId = item.ParentId,
                        EntityType = item.EntityType,
                        EntityId = item.EntityId,
                        CreatedAt = item.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = item.UpdatedAt,
                        DeletedAt = item.DeletedAt,
                        Level = 0,
                        Position = 0,
                        AccessType = "owner",
                        IsOriginal = true,
                        Data = entityData,
                        IsExpanded = false,
                        IsSelected = false,
                    });
                }

                int folderCount = itemsV2.Count(i => i.EntityType == 2);
                int noteCount   = itemsV2.Count(i => i.EntityType == 3);
                int fileCount   = itemsV2.Count(i => i.EntityType == 4);

                var response = new WorkspaceDTO
                {
                    Id = workspace.Id,
                    UserId = workspace.UserId,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    Color = "#3B82F6",
                    Icon = "📁",
                    Type = "hierarchy",
                    MaxDepth = 10,
                    IsDefault = false,
                    IsPublic = false,
                    IsTemplate = false,
                    IsArchived = false,
                    FolderCount = folderCount,
                    NoteCount = noteCount,
                    FileCount = fileCount,
                    MemberCount = 1,
                    Settings = null,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    DeletedAt = workspace.DeletedAt,
                    FlatData = itemsV2
                };

                _cache.Set(cacheKey, response, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TreeV2CacheTtl
                });

                _logger.LogInformation("Tree V2 built {ItemCount} items for workspace {WorkspaceId}",
                    itemsV2.Count, workspaceId);

                return response;
            }
            catch (KeyNotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspace tree V2 for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                throw;
            }
        }

        private Task<Dictionary<int, SuperAppModels.Models.Folder>> LoadFoldersAsync(List<int> ids) =>
            _context.Folders.AsNoTracking().IgnoreQueryFilters()
                .Where(f => ids.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

        private Task<Dictionary<int, SuperAppModels.Models.Note>> LoadNotesAsync(List<int> ids) =>
            _context.Notes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => ids.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id);

        private Task<Dictionary<int, SuperAppModels.Models.File>> LoadFilesAsync(List<int> ids) =>
            _context.Files.AsNoTracking().IgnoreQueryFilters()
                .Where(f => ids.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

        /// <summary>
        /// Creates a new folder in a workspace
        /// </summary>
        public async Task<ResultOptions> UpsertFolderAsync(int workspaceId, int userId, UpsertFolderRequest request)
        {
            try
            {
                var isUpdate = request.Id.HasValue;
                var action = isUpdate ? "Updating" : "Creating";
                
                _logger.LogInformation("{Action} folder '{Name}' in workspace {WorkspaceId} for user {UserId}, ParentId: {ParentId}",
                    action, request.Name, workspaceId, userId, request.ParentId);

                // Call repository to upsert folder
                var result = await _workspaceRepository.UpsertFolderAsync(
                    workspaceId,
                    userId,
                    request
                );

                // Check if repository operation was successful
                if (!result.Success)
                {
                    _logger.LogWarning("Repository failed to {Action} folder: {Message}", action.ToLower(), result.Message);
                    return result;
                }

                InvalidateTreeV2(workspaceId, userId);

                // Map folder object to response DTO
                var folderResponse = _mapper.Map<FolderResponse>(result.Object);

                var successMessage = isUpdate ? "Folder updated successfully" : "Folder created successfully";
                _logger.LogInformation("Successfully {Action} folder {FolderId} in workspace {WorkspaceId}",
                    action.ToLower(), result.Reference, workspaceId);

                // Return ResultOptions with mapped response
                return new ResultOptions
                {
                    Success = true,
                    Message = successMessage,
                    Reference = result.Reference,
                    Object = folderResponse,
                    Status = isUpdate ? 200 : 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting folder '{Name}' in workspace {WorkspaceId}",
                    request.Name, workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while upserting folder",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Moves multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        public async Task<ResultOptions> MoveItemsAsync(int workspaceId, int userId, MoveItemsRequest request)
        {
            try
            {
                _logger.LogInformation("Moving {Count} items in workspace {WorkspaceId} for user {UserId}",
                    request.Items.Count, workspaceId, userId);

                // Convert request items to tuple list
                var items = request.Items
                    .Select(i => (i.Type, i.Id))
                    .ToList();

                // Call repository
                var result = await _workspaceRepository.MoveItemsAsync(
                    workspaceId,
                    items,
                    request.TargetParentId,
                    request.TargetWorkspaceId
                );

                if (result.Success)
                {
                    _logger.LogInformation("Successfully moved {Count} items in workspace {WorkspaceId}",
                        request.Items.Count, workspaceId);

                    InvalidateTreeV2(workspaceId, userId);
                    if (request.TargetWorkspaceId.HasValue && request.TargetWorkspaceId.Value != workspaceId)
                    {
                        InvalidateTreeV2(request.TargetWorkspaceId.Value, userId);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to move items: {Message}", result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving items in workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while moving items",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Deletes multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        public async Task<ResultOptions> DeleteItemsAsync(int workspaceId, int userId, DeleteItemsRequest request)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} items in workspace {WorkspaceId} for user {UserId} (HardDelete: {IsHardDelete})",
                    request.Items.Count, workspaceId, userId, request.IsHardDelete);

                // Convert request items to tuple list
                var items = request.Items
                    .Select(i => (i.Type, i.Id))
                    .ToList();

                // Call repository
                var result = await _workspaceRepository.DeleteItemsAsync(workspaceId, items, request.IsHardDelete);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully deleted {Count} items in workspace {WorkspaceId}",
                        request.Items.Count, workspaceId);

                    InvalidateTreeV2(workspaceId, userId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete items: {Message}", result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting items in workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while deleting items",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Adds an item (folder/note/file) to a workspace
        /// </summary>
        public async Task<ResultOptions> AddItemToWorkspaceAsync(int workspaceId, int userId, AddItemToWorkspaceRequest request)
        {
            try
            {
                _logger.LogInformation("Adding {ChildType} (ID: {ChildId}) to workspace {WorkspaceId} under parent {ParentId}",
                    request.ChildType, request.ChildId, workspaceId, request.ParentTagId);

                // Call repository to add item
                var result = await _workspaceRepository.AddItemToWorkspaceAsync(workspaceId, userId, request);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully added {ChildType} to workspace {WorkspaceId}",
                        request.ChildType, workspaceId);

                    InvalidateTreeV2(workspaceId, userId);
                }
                else
                {
                    _logger.LogWarning("Failed to add item: {Message}", result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while adding item to workspace",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Batch upsert workspace items (soft delete/restore only)
        /// Pattern: 100% follows NoteService.UpsertNotesAsync
        /// All-or-nothing transaction - if one fails, all rollback
        /// </summary>
        public async Task<ResultOptions> UpsertWorkspaceItemsAsync(List<UpsertWorkspaceItemRequest> requests, int userId)
        {
            try
            {
                // 1. Validate input (giống NoteService)
                if (requests == null || !requests.Any())
                {
                    _logger.LogWarning("Empty batch upsert workspace items request");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No workspace items provided",
                        Status = 400
                    };
                }

                // 2. Validate business rules
                foreach (var request in requests)
                {
                    // WorkspaceId required
                    if (!request.WorkspaceId.HasValue || request.WorkspaceId.Value <= 0)
                    {
                        _logger.LogWarning("WorkspaceId is missing or invalid");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "WorkspaceId is required for all items. All changes rolled back.",
                            Status = 400
                        };
                    }

                    // UserId required
                    if (!request.UserId.HasValue || request.UserId.Value <= 0)
                    {
                        _logger.LogWarning("UserId is missing or invalid");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "UserId is required for all items. All changes rolled back.",
                            Status = 400
                        };
                    }

                    // Validate khi insert thì bắt buộc phải có data trong workspace_item
                    // KHI DATA = NULL, THÌ TỨC LÀ TA CHỈ UPDATE WORKSPACE_ITEMS THÔI
                    switch (request.EntityType)
                    {
                        case 2: // Folder
                            if (request.Id == 0 && request.FolderData == null)
                            {
                                _logger.LogWarning("FolderData is required for EntityType = 2");
                                return new ResultOptions
                                {
                                    Success = false,
                                    Message = "FolderData is required when EntityType = 2 (folder). All changes rolled back.",
                                    Status = 400
                                };
                            }
                            // Set UserId in entity data
                            request.FolderData.UserId = request.UserId.Value;
                            break;

                        case 3: // Note
                            if (request.Id == 0 &&  request.NoteData == null)
                            {
                                _logger.LogWarning("NoteData is required for EntityType = 3");
                                return new ResultOptions
                                {
                                    Success = false,
                                    Message = "NoteData is required when EntityType = 3 (note). All changes rolled back.",
                                    Status = 400
                                };
                            }
                            // Set UserId in entity data
                            request.NoteData.UserId = request.UserId.Value;
                            break;

                        case 4: // File
                            if (request.Id == 0 && request.FileData == null)
                            {
                                _logger.LogWarning("FileData is required for EntityType = 4");
                                return new ResultOptions
                                {
                                    Success = false,
                                    Message = "FileData is required when EntityType = 4 (file). All changes rolled back.",
                                    Status = 400
                                };
                            }
                            // Set UserId in entity data
                            request.FileData.UserId = request.UserId.Value;
                            break;

                        default:
                            _logger.LogWarning("Invalid EntityType: {EntityType}", request.EntityType);
                            return new ResultOptions
                            {
                                Success = false,
                                Message = $"Invalid EntityType: {request.EntityType}. Must be 2 (folder), 3 (note), or 4 (file). All changes rolled back.",
                                Status = 400
                            };
                    }
                }

                var workspaceId = requests.First().WorkspaceId!.Value;

                _logger.LogInformation("Batch upserting {Count} workspace items for workspace {WorkspaceId}, user {UserId}",
                    requests.Count, workspaceId, userId);

                // 3. Call repository layer (giống NoteService)
                var result = await _workspaceRepository.UpsertWorkspaceItemsAsync(requests, userId);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully batch upserted {Count} workspace items in workspace {WorkspaceId}",
                        requests.Count, workspaceId);

                    // Items can target multiple workspaces in a single batch — invalidate each
                    foreach (var wsId in requests.Select(r => r.WorkspaceId!.Value).Distinct())
                    {
                        InvalidateTreeV2(wsId, userId);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to batch upsert workspace items: {Message}", result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch upserting workspace items");
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while batch upserting workspace items",
                    Status = 500
                };
            }
        }
    }
}
