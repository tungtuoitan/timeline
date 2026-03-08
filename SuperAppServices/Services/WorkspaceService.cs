using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;
using SuperAppDataRepositories.Data;

namespace SuperAppServices.Services
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

        public WorkspaceService(
            IWorkspaceRepository workspaceRepository,
            IMapper mapper,
            ILogger<WorkspaceService> logger,
            ApplicationDbContext context)
        {
            _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
            try
            {
                _logger.LogInformation("Getting workspace tree V2 for WorkspaceId: {WorkspaceId}, UserId: {UserId} (NO SERVER FILTERING - all items returned)",
                    workspaceId, userId);

                // Get workspace info
                var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, userId);
                if (workspace == null)
                {
                    _logger.LogWarning("Workspace {WorkspaceId} not found for user {UserId}",
                        workspaceId, userId);
                    throw new KeyNotFoundException($"Workspace with ID {workspaceId} not found");
                }

                // ⚠️ CHANGED: Get ALL workspace items (no server-side filtering)
                // Frontend will filter by deletedAt, statusCode, and search text
                // filterOptions parameter kept for backward compatibility but not used
                var workspaceWithTree = await _workspaceRepository.GetWorkspaceTreeAsync(workspaceId, userId, null);
                if (workspaceWithTree == null)
                {
                    _logger.LogWarning("Workspace tree data not found for workspace {WorkspaceId}", workspaceId);
                    throw new InvalidOperationException($"Failed to retrieve workspace tree for workspace {workspaceId}");
                }

                // Transform to V2 structure (workspace_items properties + Data property with entity data)
                // ⚠️ CHANGED: No filters applied - returns all items
                // ✅ NEW: Queries full entity data from DB (Description, StatusCode, Type, etc.)
                var itemsV2 = await TransformToV2StructureAsync(workspaceWithTree.Items);

                // Populate workspace links for notes in the tree
                await PopulateWorkspaceLinksForTreeAsync(itemsV2);

                // Count ALL items by type (including deleted)
                int folderCount = itemsV2.Count(i => i.EntityType == 2);
                int noteCount = itemsV2.Count(i => i.EntityType == 3);
                int fileCount = itemsV2.Count(i => i.EntityType == 4);

                // Create unified WorkspaceDTO response
                var response = new WorkspaceDTO
                {
                    Id = workspace.Id,
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
                    IsArchived = false,
                    FolderCount = folderCount,
                    NoteCount = noteCount,
                    FileCount = fileCount,
                    MemberCount = 1,
                    Settings = null,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    DeletedAt = workspace.DeletedAt,
                    FlatData = itemsV2 // ✅ FLAT list with ALL items (unfiltered - frontend will apply filters)
                };

                _logger.LogInformation("Successfully retrieved workspace tree V2 with {ItemCount} items for workspace {WorkspaceId} (unfiltered - frontend will apply filters)",
                    itemsV2.Count, workspaceId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspace tree V2 for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                throw;
            }
        }

        /// <summary>
        /// Remove folders that have no children after filtering
        /// Recursive algorithm: iterate until no more empty folders
        /// </summary>
        //private List<WorkspaceItemResponseV2> RemoveEmptyFolders(List<WorkspaceItemResponseV2> items)
        //{
        //    bool removedAny;
        //    do
        //    {
        //        removedAny = false;
        //        var folderIds = items.Where(i => i.EntityType == 2).Select(i => i.Id).ToHashSet();
        //        var emptyFolderIds = new HashSet<int>();

        //        foreach (var folderId in folderIds)
        //        {
        //            // Check if folder has any children
        //            var hasChildren = items.Any(i => i.ParentId == folderId);
        //            if (!hasChildren)
        //            {
        //                emptyFolderIds.Add(folderId);
        //            }
        //        }

        //        if (emptyFolderIds.Any())
        //        {
        //            items = items.Where(i => !emptyFolderIds.Contains(i.Id)).ToList();
        //            removedAny = true;
        //        }
        //    } while (removedAny);

        //    return items;
        //}

        /// <summary>
        /// Populates workspace links for notes in the tree
        /// Similar to NoteService.PopulateWorkspaceLinksAsync
        /// </summary>
        private async Task PopulateWorkspaceLinksForTreeAsync(List<WorkspaceItemResponseV2> items)
        {
            try
            {
                // Extract all note entity IDs from the tree
                var noteEntityIds = items
                    .Where(i => i.EntityType == 3) // Notes only
                    .Select(i => i.EntityId)
                    .Distinct()
                    .ToList();

                if (!noteEntityIds.Any())
                {
                    return; // No notes in tree
                }

                // Query workspace_items to find all workspaces that link to these notes
                var workspaceLinks = await _context.WorkspaceItems
                    .Where(wi => wi.EntityType == 3 && noteEntityIds.Contains(wi.EntityId))
                    .Join(_context.Workspaces,
                        wi => wi.WorkspaceId,
                        w => w.Id,
                        (wi, w) => new
                        {
                            NoteEntityId = wi.EntityId,
                            WorkspaceId = w.Id,
                            WorkspaceName = w.Name,
                            WorkspaceItemId = wi.Id
                        })
                    .ToListAsync();

                // Group by note entity ID
                var linksByNoteId = workspaceLinks
                    .GroupBy(x => x.NoteEntityId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => new WorkspaceLinkDTO
                        {
                            WorkspaceId = x.WorkspaceId,
                            WorkspaceName = x.WorkspaceName,
                            WorkspaceItemId = x.WorkspaceItemId
                        }).ToList()
                    );

                // Populate workspace links into each note's Data
                foreach (var item in items.Where(i => i.EntityType == 3))
                {
                    if (item.Data is NoteData noteData && linksByNoteId.TryGetValue(item.EntityId, out var links))
                    {
                        noteData.WorkspaceLinks = links;
                    }
                }

                _logger.LogInformation("Populated workspace links for {NoteCount} notes", noteEntityIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating workspace links for tree notes");
                // Don't throw - this is a non-critical feature
            }
        }

        /// <summary>
        /// Transforms WorkspaceItem (mixed structure) to WorkspaceItemResponseV2 (clear separation)
        /// Reorganizes data: workspace_items properties at root + entity data in 'Data' property
        /// Queries full entity data from DB to populate Description, Type, StatusCode, etc.
        /// </summary>
        private async Task<List<WorkspaceItemResponseV2>> TransformToV2StructureAsync(List<SuperAppModels.Models.WorkspaceItem> items)
        {
            // ===== STEP 1: Preload full entity data from DB =====
            // Extract entity IDs by type
            var folderIds = items.Where(i => i.Type.ToLowerInvariant() == "folder").Select(i => (int)i.ItemId).Distinct().ToList();
            var noteIds = items.Where(i => i.Type.ToLowerInvariant() == "note").Select(i => (int)i.ItemId).Distinct().ToList();
            var fileIds = items.Where(i => i.Type.ToLowerInvariant() == "file").Select(i => (int)i.ItemId).Distinct().ToList();

            // Query full entity data (Description, Type, StatusCode, etc.)
            var foldersDict = folderIds.Any()
                ? await _context.Folders.AsNoTracking().Where(f => folderIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id)
                : new Dictionary<int, SuperAppModels.Models.Folder>();

            var notesDict = noteIds.Any()
                ? await _context.Notes.AsNoTracking().Where(n => noteIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id)
                : new Dictionary<int, SuperAppModels.Models.Note>();

            var filesDict = fileIds.Any()
                ? await _context.Files.AsNoTracking().Where(f => fileIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id)
                : new Dictionary<int, SuperAppModels.Models.File>();

            // ===== STEP 2: Transform items with full entity data =====
            return items.Select(item =>
            {
                // Determine EntityType byte value
                byte entityType = item.Type.ToLowerInvariant() switch
                {
                    "folder" => 2,
                    "note" => 3,
                    "file" => 4,
                    _ => throw new InvalidOperationException($"Unknown item type: {item.Type}")
                };

                // Create entity data object based on type WITH FULL DATA FROM DB
                object entityData = entityType switch
                {
                    2 when foldersDict.TryGetValue((int)item.ItemId, out var folder) => new FolderData
                    {
                        Id = folder.Id,
                        UserId = folder.UserId,
                        Name = folder.Name,
                        Description = folder.Description, // ✅ FROM DB
                        Color = folder.Color,
                        Icon = folder.Icon,
                        CreatedAt = folder.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = folder.UpdatedAt,
                        DeletedAt = folder.DeletedAt
                    },
                    3 when notesDict.TryGetValue((int)item.ItemId, out var note) => new NoteData
                    {
                        Id = note.Id,
                        UserId = note.UserId,
                        Name = note.Name,
                        Description = note.Description, // ✅ FROM DB
                        StatusCode = note.StatusCode,   // ✅ FROM DB
                        Icon = note.Icon,               // ✅ FROM DB
                        Color = note.Color,             // ✅ FROM DB
                        CreatedAt = note.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = note.UpdatedAt,
                        DeletedAt = note.DeletedAt
                    },
                    4 when filesDict.TryGetValue((int)item.ItemId, out var file) => new FileData
                    {
                        Id = file.Id,
                        UserId = file.UserId,
                        Name = file.Name,
                        Url = file.Url,             // ✅ FROM DB
                        FileSize = file.FileSize,   // ✅ FROM DB
                        MimeType = file.MimeType,   // ✅ FROM DB
                        Extension = file.Extension, // ✅ FROM DB
                        StatusCode = file.StatusCode, // ✅ FROM DB
                        CreatedAt = file.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = file.UpdatedAt,
                        DeletedAt = file.DeletedAt
                    },
                    _ => throw new InvalidOperationException($"Unsupported or missing entity data for type: {entityType}")
                };

                // Build WorkspaceItemResponseV2 with clear separation
                return new WorkspaceItemResponseV2
                {
                    // ============ FROM workspace_items TABLE ============
                    Id = item.RelationshipId ?? 0,
                    WorkspaceId = 0, // Will be set from workspace context
                    ParentId = item.ParentId,
                    EntityType = entityType,
                    EntityId = (int)item.ItemId,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    DeletedAt = item.DeletedAt,

                    // ============ COMPUTED PROPERTIES ============
                    Level = item.Level,
                    Position = item.Position,
                    AccessType = item.AccessType,
                    IsOriginal = item.IsOriginal,

                    // ============ ENTITY DATA ============
                    Data = entityData,

                    // ============ UI STATE ============
                    IsExpanded = false,
                    IsSelected = false
                };
            }).ToList();
        }

       

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
