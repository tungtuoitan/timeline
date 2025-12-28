using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SuperAppModels.Models;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppDataRepositories.Data;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for managing workspace items with action-based batch operations
    /// Handles 6 explicit actions: Create, Add, Move, Update, Delete, Restore
    /// Pattern: 100% follows NoteRepository.UpsertNotesAsync (preload, validate, process, single save)
    /// </summary>
    public class WorkspaceItemService : IWorkspaceItemService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkspaceItemService> _logger;

        public WorkspaceItemService(
            ApplicationDbContext context,
            ILogger<WorkspaceItemService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Batch upsert workspace items with transaction management and action-based validation
        /// Pattern: Preload → Validate → Process → Single SaveChanges
        /// </summary>
        public async Task<ResultOptions> UpsertWorkspaceItemsAsync(
            List<UpsertWorkspaceItemRequest> requests,
            int userId,
            int workspaceId)
        {
            // Use execution strategy to handle retry logic with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ===== STEP 1: Basic Validation =====
                    if (requests == null || !requests.Any())
                    {
                        await transaction.RollbackAsync();
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "No workspace items provided",
                            Status = 400
                        };
                    }

                    _logger.LogInformation(
                        "Starting batch upsert transaction for {Count} workspace items in workspace {WorkspaceId}",
                        requests.Count, workspaceId);

                    // ===== STEP 2: PRELOAD ALL DATA (Batch Queries - No N+1!) =====

                    // Preload workspace items for Update/Move/Delete/Restore actions
                    var itemIdsToUpdate = requests
                        .Where(r => r.Action == WorkspaceItemAction.Move ||
                                   r.Action == WorkspaceItemAction.Update ||
                                   r.Action == WorkspaceItemAction.Delete ||
                                   r.Action == WorkspaceItemAction.Restore)
                        .Where(r => r.Id.HasValue && r.Id.Value > 0)
                        .Select(r => r.Id.Value)
                        .Distinct()
                        .ToList();

                    var existingItemsDict = await _context.WorkspaceItems
                        .IgnoreQueryFilters()
                        .Where(wi => itemIdsToUpdate.Contains(wi.Id))
                        .ToDictionaryAsync(wi => wi.Id, wi => wi);

                    // Preload entity IDs for Add action (verify existing entities)
                    var folderIdsToAdd = requests
                        .Where(r => r.Action == WorkspaceItemAction.Add && r.ItemType == 2 && r.ItemId.HasValue)
                        .Select(r => r.ItemId.Value)
                        .Distinct()
                        .ToList();

                    var noteIdsToAdd = requests
                        .Where(r => r.Action == WorkspaceItemAction.Add && r.ItemType == 3 && r.ItemId.HasValue)
                        .Select(r => r.ItemId.Value)
                        .Distinct()
                        .ToList();

                    var fileIdsToAdd = requests
                        .Where(r => r.Action == WorkspaceItemAction.Add && r.ItemType == 4 && r.ItemId.HasValue)
                        .Select(r => r.ItemId.Value)
                        .Distinct()
                        .ToList();

                    var existingFolderIds = folderIdsToAdd.Any()
                        ? await _context.Folders.Where(f => folderIdsToAdd.Contains(f.Id)).Select(f => f.Id).ToListAsync()
                        : new List<int>();

                    var existingNoteIds = noteIdsToAdd.Any()
                        ? await _context.Notes.Where(n => noteIdsToAdd.Contains(n.Id)).Select(n => n.Id).ToListAsync()
                        : new List<int>();

                    var existingFileIds = fileIdsToAdd.Any()
                        ? await _context.Files.Where(f => fileIdsToAdd.Contains(f.Id)).Select(f => f.Id).ToListAsync()
                        : new List<int>();

                    // Preload entities for Update action
                    var folderIdsToUpdate = requests
                        .Where(r => r.Action == WorkspaceItemAction.Update && r.Id.HasValue)
                        .Select(r => r.Id.Value)
                        .Where(id => existingItemsDict.ContainsKey(id) && existingItemsDict[id].ItemType == 2)
                        .Select(id => existingItemsDict[id].ItemId)
                        .Distinct()
                        .ToList();

                    var noteIdsToUpdate = requests
                        .Where(r => r.Action == WorkspaceItemAction.Update && r.Id.HasValue)
                        .Select(r => r.Id.Value)
                        .Where(id => existingItemsDict.ContainsKey(id) && existingItemsDict[id].ItemType == 3)
                        .Select(id => existingItemsDict[id].ItemId)
                        .Distinct()
                        .ToList();

                    var fileIdsToUpdate = requests
                        .Where(r => r.Action == WorkspaceItemAction.Update && r.Id.HasValue)
                        .Select(r => r.Id.Value)
                        .Where(id => existingItemsDict.ContainsKey(id) && existingItemsDict[id].ItemType == 4)
                        .Select(id => existingItemsDict[id].ItemId)
                        .Distinct()
                        .ToList();

                    var foldersToUpdateDict = folderIdsToUpdate.Any()
                        ? await _context.Folders.Where(f => folderIdsToUpdate.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f)
                        : new Dictionary<int, Folder>();

                    var notesToUpdateDict = noteIdsToUpdate.Any()
                        ? await _context.Notes.Where(n => noteIdsToUpdate.Contains(n.Id)).ToDictionaryAsync(n => n.Id, n => n)
                        : new Dictionary<int, Note>();

                    var filesToUpdateDict = fileIdsToUpdate.Any()
                        ? await _context.Files.Where(f => fileIdsToUpdate.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f)
                        : new Dictionary<int, SuperAppModels.Models.File>();

                    // ===== STEP 3: VALIDATE ALL REQUESTS (Fail-Fast) =====

                    var validationErrors = new List<string>();

                    foreach (var request in requests)
                    {
                        var validationResult = ValidateRequest(
                            request,
                            existingItemsDict,
                            existingFolderIds,
                            existingNoteIds,
                            existingFileIds,
                            foldersToUpdateDict,
                            notesToUpdateDict,
                            filesToUpdateDict);

                        if (!string.IsNullOrEmpty(validationResult))
                        {
                            validationErrors.Add(validationResult);
                        }
                    }

                    // Check circular dependencies for Move action
                    var moveRequests = requests.Where(r => r.Action == WorkspaceItemAction.Move).ToList();
                    foreach (var moveRequest in moveRequests)
                    {
                        if (moveRequest.Id.HasValue && moveRequest.ParentId.HasValue)
                        {
                            var circularCheck = await CheckCircularDependencyAsync(
                                moveRequest.Id.Value,
                                moveRequest.ParentId.Value,
                                existingItemsDict);

                            if (circularCheck != null)
                            {
                                validationErrors.Add(circularCheck);
                            }
                        }
                    }

                    if (validationErrors.Any())
                    {
                        await transaction.RollbackAsync();
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"Validation failed: {string.Join("; ", validationErrors)}",
                            Status = 400
                        };
                    }

                    // ===== STEP 4: PROCESS ALL REQUESTS (Track Changes, Don't Save Yet!) =====

                    var upsertedItems = new List<WorkspaceItemEntity>();

                    foreach (var request in requests)
                    {
                        switch (request.Action)
                        {
                            case WorkspaceItemAction.Create:
                                await ProcessCreateActionAsync(request, userId, workspaceId, upsertedItems);
                                break;

                            case WorkspaceItemAction.Add:
                                ProcessAddAction(request, workspaceId, upsertedItems);
                                break;

                            case WorkspaceItemAction.Move:
                                ProcessMoveAction(request, existingItemsDict, upsertedItems);
                                break;

                            case WorkspaceItemAction.Update:
                                ProcessUpdateAction(
                                    request,
                                    existingItemsDict,
                                    foldersToUpdateDict,
                                    notesToUpdateDict,
                                    filesToUpdateDict);
                                break;

                            case WorkspaceItemAction.Delete:
                                ProcessDeleteAction(request, existingItemsDict, upsertedItems);
                                break;

                            case WorkspaceItemAction.Restore:
                                ProcessRestoreAction(request, existingItemsDict, upsertedItems);
                                break;

                            default:
                                throw new ArgumentException($"Unsupported action: {request.Action}");
                        }
                    }

                    // ===== STEP 5: SINGLE SAVECHANGES (Transaction Atomicity!) =====
                    await _context.SaveChangesAsync();

                    // ===== STEP 6: Commit Transaction =====
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Successfully committed batch upsert transaction for {Count} workspace items",
                        requests.Count);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully processed {requests.Count} workspace items in batch",
                        Data = upsertedItems.Cast<object>().ToList(),
                        Status = 200
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency conflict during batch upsert workspace items");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Concurrency conflict - all changes rolled back",
                        Status = 409
                    };
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database update error during batch upsert workspace items");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"Database update error - all changes rolled back. {ex.InnerException?.Message}",
                        Status = 500
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Unexpected error during batch upsert workspace items");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"An error occurred - all changes rolled back. {ex.Message}",
                        Status = 500
                    };
                }
            });
        }

        // =====================================================================
        // VALIDATION METHODS
        // =====================================================================

        /// <summary>
        /// Validate request based on action type
        /// Returns error message if invalid, null if valid
        /// </summary>
        private string? ValidateRequest(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<int> existingFolderIds,
            List<int> existingNoteIds,
            List<int> existingFileIds,
            Dictionary<int, Folder> foldersToUpdateDict,
            Dictionary<int, Note> notesToUpdateDict,
            Dictionary<int, SuperAppModels.Models.File> filesToUpdateDict)
        {
            switch (request.Action)
            {
                case WorkspaceItemAction.Create:
                    // Required: ItemType, EntityData
                    if (!request.ItemType.HasValue)
                        return "Create action requires ItemType";

                    if (request.ItemType.Value < 2 || request.ItemType.Value > 4)
                        return $"Invalid ItemType: {request.ItemType}";

                    if (!HasEntityData(request))
                        return $"Create action requires entity data for ItemType {request.ItemType}";

                    return null;

                case WorkspaceItemAction.Add:
                    // Required: ItemType, ItemId
                    if (!request.ItemType.HasValue)
                        return "Add action requires ItemType";

                    if (!request.ItemId.HasValue || request.ItemId.Value <= 0)
                        return "Add action requires valid ItemId";

                    // Validate entity exists
                    var entityExists = request.ItemType.Value switch
                    {
                        2 => existingFolderIds.Contains(request.ItemId.Value),
                        3 => existingNoteIds.Contains(request.ItemId.Value),
                        4 => existingFileIds.Contains(request.ItemId.Value),
                        _ => false
                    };

                    if (!entityExists)
                        return $"Entity with ItemType={request.ItemType} and ItemId={request.ItemId} not found";

                    return null;

                case WorkspaceItemAction.Move:
                    // Required: Id + (ParentId OR WorkspaceId)
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Move action requires valid Id";

                    if (!request.ParentId.HasValue && !request.WorkspaceId.HasValue)
                        return "Move action requires either ParentId or WorkspaceId";

                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";

                    return null;

                case WorkspaceItemAction.Update:
                    // Required: Id, EntityData
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Update action requires valid Id";

                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";

                    var workspaceItem = existingItemsDict[request.Id.Value];

                    // Validate entity exists for update
                    var entityExistsForUpdate = workspaceItem.ItemType switch
                    {
                        2 => foldersToUpdateDict.ContainsKey(workspaceItem.ItemId),
                        3 => notesToUpdateDict.ContainsKey(workspaceItem.ItemId),
                        4 => filesToUpdateDict.ContainsKey(workspaceItem.ItemId),
                        _ => false
                    };

                    if (!entityExistsForUpdate)
                        return $"Entity with ItemType={workspaceItem.ItemType} and ItemId={workspaceItem.ItemId} not found for update";

                    if (!HasEntityData(request))
                        return "Update action requires entity data";

                    return null;

                case WorkspaceItemAction.Delete:
                    // Required: Id
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Delete action requires valid Id";

                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";

                    return null;

                case WorkspaceItemAction.Restore:
                    // Required: Id
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Restore action requires valid Id";

                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";

                    var itemToRestore = existingItemsDict[request.Id.Value];
                    if (itemToRestore.DeletedAt == null)
                        return $"Workspace item ID {request.Id} is not deleted, cannot restore";

                    return null;

                default:
                    return $"Unsupported action: {request.Action}";
            }
        }

        /// <summary>
        /// Check for circular dependency when moving an item
        /// Returns error message if circular dependency detected, null if safe
        /// </summary>
        private async Task<string?> CheckCircularDependencyAsync(
            int itemId,
            int newParentId,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict)
        {
            // Can't move item to itself
            if (itemId == newParentId)
                return $"Cannot move item {itemId} to itself";

            // Check if newParentId is a descendant of itemId (would create cycle)
            var currentParentId = newParentId;
            var visited = new HashSet<int> { itemId };
            var maxDepth = 100; // Prevent infinite loop
            var depth = 0;

            while (currentParentId > 0 && depth < maxDepth)
            {
                if (visited.Contains(currentParentId))
                    return $"Circular dependency detected: moving item {itemId} to parent {newParentId} would create a cycle";

                visited.Add(currentParentId);

                // Get parent from preloaded dict or database
                WorkspaceItemEntity? parent = null;
                if (existingItemsDict.ContainsKey(currentParentId))
                {
                    parent = existingItemsDict[currentParentId];
                }
                else
                {
                    parent = await _context.WorkspaceItems.FindAsync(currentParentId);
                }

                if (parent == null || !parent.ParentId.HasValue)
                    break;

                currentParentId = parent.ParentId.Value;
                depth++;
            }

            if (depth >= maxDepth)
                return $"Maximum tree depth exceeded when checking circular dependency for item {itemId}";

            return null; // No circular dependency
        }

        /// <summary>
        /// Check if request has entity data (for CREATE/UPDATE actions)
        /// </summary>
        private bool HasEntityData(UpsertWorkspaceItemRequest request)
        {
            if (!request.ItemType.HasValue)
                return false;

            return request.ItemType.Value switch
            {
                2 => request.FolderData != null,
                3 => request.NoteData != null,
                4 => request.FileData != null,
                _ => false
            };
        }

        // =====================================================================
        // ACTION PROCESSING METHODS (Track Changes Only, No SaveChanges!)
        // =====================================================================

        /// <summary>
        /// CREATE: Create new entity + workspace_item
        /// </summary>
        private async Task ProcessCreateActionAsync(
            UpsertWorkspaceItemRequest request,
            int userId,
            int workspaceId,
            List<WorkspaceItemEntity> upsertedItems)
        {
            _logger.LogInformation(
                "Processing CREATE action: ItemType={ItemType}",
                request.ItemType);

            // Create entity (just track, don't save!)
            int entityId = 0;
            switch (request.ItemType!.Value)
            {
                case 2: // Folder
                    var folderData = request.FolderData!;
                    var newFolder = new Folder
                    {
                        UserId = userId,
                        Name = folderData.Name,
                        Description = folderData.Description,
                        Color = folderData.Color,
                        Icon = folderData.Icon,
                        CreatedAt = DateTime.UtcNow,
                        DeletedAt = null
                    };
                    _context.Folders.Add(newFolder);
                    // Note: ID will be generated after SaveChangesAsync
                    // We need to save to get the ID for workspace_item
                    await _context.SaveChangesAsync();
                    entityId = newFolder.Id;
                    break;

                case 3: // Note
                    var noteData = request.NoteData!;
                    var newNote = new Note
                    {
                        UserId = userId,
                        Name = noteData.Name,
                        Description = noteData.Description,
                        StatusCode = noteData.StatusCode,
                        CreatedAt = DateTime.UtcNow,
                        DeletedAt = null
                    };
                    _context.Notes.Add(newNote);
                    await _context.SaveChangesAsync();
                    entityId = newNote.Id;
                    break;

                case 4: // File
                    var fileData = request.FileData!;
                    var newFile = new SuperAppModels.Models.File
                    {
                        UserId = userId,
                        Name = fileData.Name,
                        Url = fileData.Url,
                        FileSize = fileData.FileSize,
                        MimeType = fileData.MimeType,
                        Extension = fileData.Extension,
                        StatusCode = fileData.StatusCode,
                        CreatedAt = DateTime.UtcNow,
                        DeletedAt = null
                    };
                    _context.Files.Add(newFile);
                    await _context.SaveChangesAsync();
                    entityId = newFile.Id;
                    break;
            }

            // Create workspace_item
            var newItem = new WorkspaceItemEntity
            {
                WorkspaceId = request.WorkspaceId ?? workspaceId,
                ParentId = request.ParentId,
                ItemType = request.ItemType.Value,
                ItemId = entityId,
                CopyInfo = request.CopyInfo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                DeletedAt = null
            };

            _context.WorkspaceItems.Add(newItem);
            upsertedItems.Add(newItem);

            _logger.LogInformation(
                "Created entity ID {EntityId} and workspace_item for ItemType {ItemType}",
                entityId, request.ItemType);
        }

        /// <summary>
        /// ADD: Add existing entity to workspace
        /// </summary>
        private void ProcessAddAction(
            UpsertWorkspaceItemRequest request,
            int workspaceId,
            List<WorkspaceItemEntity> upsertedItems)
        {
            _logger.LogInformation(
                "Processing ADD action: ItemType={ItemType}, ItemId={ItemId}",
                request.ItemType, request.ItemId);

            var newItem = new WorkspaceItemEntity
            {
                WorkspaceId = request.WorkspaceId ?? workspaceId,
                ParentId = request.ParentId,
                ItemType = request.ItemType!.Value,
                ItemId = request.ItemId!.Value,
                CopyInfo = request.CopyInfo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                DeletedAt = null
            };

            _context.WorkspaceItems.Add(newItem);
            upsertedItems.Add(newItem);

            _logger.LogInformation(
                "Added existing entity ID {ItemId} to workspace as workspace_item",
                request.ItemId);
        }

        /// <summary>
        /// MOVE: Change workspace_item location
        /// </summary>
        private void ProcessMoveAction(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<WorkspaceItemEntity> upsertedItems)
        {
            _logger.LogInformation(
                "Processing MOVE action: Id={Id}, ParentId={ParentId}, WorkspaceId={WorkspaceId}",
                request.Id, request.ParentId, request.WorkspaceId);

            var existingItem = existingItemsDict[request.Id!.Value];

            // Update location
            if (request.ParentId.HasValue)
                existingItem.ParentId = request.ParentId;

            if (request.WorkspaceId.HasValue)
                existingItem.WorkspaceId = request.WorkspaceId.Value;

            existingItem.UpdatedAt = DateTime.UtcNow;
            upsertedItems.Add(existingItem);

            _logger.LogInformation(
                "Moved workspace_item ID {WorkspaceItemId} to ParentId={ParentId}, WorkspaceId={WorkspaceId}",
                request.Id, existingItem.ParentId, existingItem.WorkspaceId);
        }

        /// <summary>
        /// UPDATE: Update entity data
        /// </summary>
        private void ProcessUpdateAction(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            Dictionary<int, Folder> foldersToUpdateDict,
            Dictionary<int, Note> notesToUpdateDict,
            Dictionary<int, SuperAppModels.Models.File> filesToUpdateDict)
        {
            _logger.LogInformation(
                "Processing UPDATE action: Id={Id}",
                request.Id);

            var workspaceItem = existingItemsDict[request.Id!.Value];

            switch (workspaceItem.ItemType)
            {
                case 2: // Folder
                    var folder = foldersToUpdateDict[workspaceItem.ItemId];
                    var folderData = request.FolderData!;
                    folder.Name = folderData.Name;
                    folder.Description = folderData.Description;
                    folder.Color = folderData.Color;
                    folder.Icon = folderData.Icon;
                    folder.UpdatedAt = DateTime.UtcNow;
                    if (folderData.DeletedAt.HasValue)
                        folder.DeletedAt = folderData.DeletedAt;
                    break;

                case 3: // Note
                    var note = notesToUpdateDict[workspaceItem.ItemId];
                    var noteData = request.NoteData!;
                    note.Name = noteData.Name;
                    note.Description = noteData.Description;
                    note.StatusCode = noteData.StatusCode;
                    note.UpdatedAt = DateTime.UtcNow;
                    if (noteData.DeletedAt.HasValue)
                        note.DeletedAt = noteData.DeletedAt;
                    break;

                case 4: // File
                    var file = filesToUpdateDict[workspaceItem.ItemId];
                    var fileData = request.FileData!;
                    file.Name = fileData.Name;
                    file.Url = fileData.Url;
                    file.FileSize = fileData.FileSize;
                    file.MimeType = fileData.MimeType;
                    file.Extension = fileData.Extension;
                    file.StatusCode = fileData.StatusCode;
                    file.UpdatedAt = DateTime.UtcNow;
                    if (fileData.DeletedAt.HasValue)
                        file.DeletedAt = fileData.DeletedAt;
                    break;
            }

            _logger.LogInformation(
                "Updated entity for workspace_item ID {WorkspaceItemId}",
                request.Id);
        }

        /// <summary>
        /// DELETE: Soft delete workspace_item
        /// </summary>
        private void ProcessDeleteAction(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<WorkspaceItemEntity> upsertedItems)
        {
            _logger.LogInformation(
                "Processing DELETE action: Id={Id}",
                request.Id);

            var existingItem = existingItemsDict[request.Id!.Value];
            existingItem.DeletedAt = DateTime.UtcNow;
            existingItem.UpdatedAt = DateTime.UtcNow;
            upsertedItems.Add(existingItem);

            _logger.LogInformation(
                "Soft deleted workspace_item ID {WorkspaceItemId}",
                request.Id);
        }

        /// <summary>
        /// RESTORE: Restore deleted workspace_item
        /// </summary>
        private void ProcessRestoreAction(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<WorkspaceItemEntity> upsertedItems)
        {
            _logger.LogInformation(
                "Processing RESTORE action: Id={Id}",
                request.Id);

            var existingItem = existingItemsDict[request.Id!.Value];
            existingItem.DeletedAt = null;
            existingItem.UpdatedAt = DateTime.UtcNow;
            upsertedItems.Add(existingItem);

            _logger.LogInformation(
                "Restored workspace_item ID {WorkspaceItemId}",
                request.Id);
        }
    }
}
