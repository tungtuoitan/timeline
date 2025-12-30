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
                                   r.Action == WorkspaceItemAction.MoveCross ||
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
                        .Where(r => r.Action == WorkspaceItemAction.Add && r.EntityType == 2 && r.EntityId.HasValue)
                        .Select(r => r.EntityId.Value)
                        .Distinct()
                        .ToList();

                    var noteIdsToAdd = requests
                        .Where(r => r.Action == WorkspaceItemAction.Add && r.EntityType == 3 && r.EntityId.HasValue)
                        .Select(r => r.EntityId.Value)
                        .Distinct()
                        .ToList();

                    var fileIdsToAdd = requests
                        .Where(r => r.Action == WorkspaceItemAction.Add && r.EntityType == 4 && r.EntityId.HasValue)
                        .Select(r => r.EntityId.Value)
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
                        .Where(id => existingItemsDict.ContainsKey(id) && existingItemsDict[id].EntityType == 2)
                        .Select(id => existingItemsDict[id].EntityId)
                        .Distinct()
                        .ToList();

                    var noteIdsToUpdate = requests
                        .Where(r => r.Action == WorkspaceItemAction.Update && r.Id.HasValue)
                        .Select(r => r.Id.Value)
                        .Where(id => existingItemsDict.ContainsKey(id) && existingItemsDict[id].EntityType == 3)
                        .Select(id => existingItemsDict[id].EntityId)
                        .Distinct()
                        .ToList();

                    var fileIdsToUpdate = requests
                        .Where(r => r.Action == WorkspaceItemAction.Update && r.Id.HasValue)
                        .Select(r => r.Id.Value)
                        .Where(id => existingItemsDict.ContainsKey(id) && existingItemsDict[id].EntityType == 4)
                        .Select(id => existingItemsDict[id].EntityId)
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

                            case WorkspaceItemAction.MoveCross:
                                await ProcessMoveCrossActionAsync(request, existingItemsDict, upsertedItems);
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
                    // Required: EntityType, EntityData
                    if (!request.EntityType.HasValue)
                        return "Create action requires EntityType";

                    if (request.EntityType.Value < 2 || request.EntityType.Value > 4)
                        return $"Invalid EntityType: {request.EntityType}";

                    if (!HasEntityData(request))
                        return $"Create action requires entity data for EntityType {request.EntityType}";

                    return null;

                case WorkspaceItemAction.Add:
                    // Required: EntityType, EntityId
                    if (!request.EntityType.HasValue)
                        return "Add action requires EntityType";

                    if (!request.EntityId.HasValue || request.EntityId.Value <= 0)
                        return "Add action requires valid EntityId";

                    // Validate entity exists
                    var entityExists = request.EntityType.Value switch
                    {
                        2 => existingFolderIds.Contains(request.EntityId.Value),
                        3 => existingNoteIds.Contains(request.EntityId.Value),
                        4 => existingFileIds.Contains(request.EntityId.Value),
                        _ => false
                    };

                    if (!entityExists)
                        return $"Entity with EntityType={request.EntityType} and EntityId={request.EntityId} not found";

                    return null;

                case WorkspaceItemAction.Move:
                    // Required: Id + ParentId (within same workspace)
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Move action requires valid Id";

                    //if (!request.ParentId.HasValue)
                    //    return "Move action requires ParentId";

                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";

                    return null;

                case WorkspaceItemAction.MoveCross:
                    // Required: Id + WorkspaceId (target workspace)
                    // Optional: ParentId (target parent in new workspace, null = root)
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "MoveCross action requires valid Id";

                    if (!request.WorkspaceId.HasValue || request.WorkspaceId.Value <= 0)
                        return "MoveCross action requires valid target WorkspaceId";

                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";

                    // Validate that target workspace is different from source workspace
                    var sourceItem = existingItemsDict[request.Id.Value];
                    if (sourceItem.WorkspaceId == request.WorkspaceId.Value)
                        return "MoveCross requires target workspace to be different from source workspace";

                    return null;

                case WorkspaceItemAction.Update:
                    // Required: Id, EntityData
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Update action requires valid Id";

                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";

                    var workspaceItem = existingItemsDict[request.Id.Value];

                    // Validate entity exists for update
                    var entityExistsForUpdate = workspaceItem.EntityType switch
                    {
                        2 => foldersToUpdateDict.ContainsKey(workspaceItem.EntityId),
                        3 => notesToUpdateDict.ContainsKey(workspaceItem.EntityId),
                        4 => filesToUpdateDict.ContainsKey(workspaceItem.EntityId),
                        _ => false
                    };

                    if (!entityExistsForUpdate)
                        return $"Entity with EntityType={workspaceItem.EntityType} and EntityId={workspaceItem.EntityId} not found for update";

                    // For UPDATE, use EntityType from existing workspaceItem
                    if (!HasEntityData(request, workspaceItem.EntityType))
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
        /// <param name="request">Workspace item request</param>
        /// <param name="entityType">Override EntityType (for UPDATE action where EntityType comes from existing item)</param>
        private bool HasEntityData(UpsertWorkspaceItemRequest request, byte? entityType = null)
        {
            // Use provided entityType (UPDATE) or request.EntityType (CREATE)
            var typeToCheck = entityType ?? request.EntityType;

            if (!typeToCheck.HasValue)
                return false;

            return typeToCheck.Value switch
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
                "Processing CREATE action: EntityType={EntityType}",
                request.EntityType);

            // Create entity (just track, don't save!)
            int entityId = 0;
            switch (request.EntityType!.Value)
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
                EntityType = request.EntityType.Value,
                EntityId = entityId,
                CopyInfo = request.CopyInfo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                DeletedAt = null
            };

            _context.WorkspaceItems.Add(newItem);
            upsertedItems.Add(newItem);

            _logger.LogInformation(
                "Created entity ID {EntityId} and workspace_item for EntityType {EntityType}",
                entityId, request.EntityType);
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
                "Processing ADD action: EntityType={EntityType}, EntityId={EntityId}",
                request.EntityType, request.EntityId);

            var newItem = new WorkspaceItemEntity
            {
                WorkspaceId = request.WorkspaceId ?? workspaceId,
                ParentId = request.ParentId,
                EntityType = request.EntityType!.Value,
                EntityId = request.EntityId!.Value,
                CopyInfo = request.CopyInfo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                DeletedAt = null
            };

            _context.WorkspaceItems.Add(newItem);
            upsertedItems.Add(newItem);

            _logger.LogInformation(
                "Added existing entity ID {EntityId} to workspace as workspace_item",
                request.EntityId);
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
            existingItem.ParentId = request.ParentId; // null thì set null

            if (request.WorkspaceId.HasValue)
                existingItem.WorkspaceId = request.WorkspaceId.Value;

            existingItem.UpdatedAt = DateTime.UtcNow;
            upsertedItems.Add(existingItem);

            _logger.LogInformation(
                "Moved workspace_item ID {WorkspaceItemId} to ParentId={ParentId}, WorkspaceId={WorkspaceId}",
                request.Id, existingItem.ParentId, existingItem.WorkspaceId);
        }

        /// <summary>
        /// MOVE CROSS: Move workspace_item to another workspace with all descendants
        /// </summary>
        private async Task ProcessMoveCrossActionAsync(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            List<WorkspaceItemEntity> upsertedItems)
        {
            _logger.LogInformation(
                "Processing MOVE_CROSS action: Id={Id}, TargetWorkspaceId={WorkspaceId}, TargetParentId={ParentId}",
                request.Id, request.WorkspaceId, request.ParentId);

            var rootItem = existingItemsDict[request.Id!.Value];
            var targetWorkspaceId = request.WorkspaceId!.Value;
            var targetParentId = request.ParentId; // null = root level in target workspace

            // ===== STEP 1: Update root item =====
            rootItem.WorkspaceId = targetWorkspaceId;
            rootItem.ParentId = targetParentId;
            rootItem.UpdatedAt = DateTime.UtcNow;
            upsertedItems.Add(rootItem);

            _logger.LogInformation(
                "Updated root item {WorkspaceItemId} to workspace {TargetWorkspaceId}",
                rootItem.Id, targetWorkspaceId);

            // ===== STEP 2: Find and update all descendants recursively =====
            var allDescendants = await GetAllDescendantsAsync(rootItem.Id);

            _logger.LogInformation(
                "Found {Count} descendants to move with root item {WorkspaceItemId}",
                allDescendants.Count, rootItem.Id);

            foreach (var descendant in allDescendants)
            {
                descendant.WorkspaceId = targetWorkspaceId;
                descendant.UpdatedAt = DateTime.UtcNow;
                upsertedItems.Add(descendant);

                _logger.LogDebug(
                    "Updated descendant {WorkspaceItemId} to workspace {TargetWorkspaceId}",
                    descendant.Id, targetWorkspaceId);
            }

            _logger.LogInformation(
                "Completed MOVE_CROSS: Moved {TotalCount} items (1 root + {DescendantsCount} descendants) to workspace {TargetWorkspaceId}",
                allDescendants.Count + 1, allDescendants.Count, targetWorkspaceId);
        }

        /// <summary>
        /// Recursively get all descendants of a workspace item
        /// </summary>
        private async Task<List<WorkspaceItemEntity>> GetAllDescendantsAsync(int parentWorkspaceItemId)
        {
            var descendants = new List<WorkspaceItemEntity>();
            var queue = new Queue<int>();
            queue.Enqueue(parentWorkspaceItemId);

            while (queue.Count > 0)
            {
                var currentParentId = queue.Dequeue();

                // Find direct children
                var children = await _context.WorkspaceItems
                    .Where(wi => wi.ParentId == currentParentId)
                    .ToListAsync();

                foreach (var child in children)
                {
                    descendants.Add(child);
                    queue.Enqueue(child.Id); // Add to queue to process its children
                }
            }

            return descendants;
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

            switch (workspaceItem.EntityType)
            {
                case 2: // Folder
                    var folder = foldersToUpdateDict[workspaceItem.EntityId];
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
                    var note = notesToUpdateDict[workspaceItem.EntityId];
                    var noteData = request.NoteData!;
                    note.Name = noteData.Name;
                    note.Description = noteData.Description;
                    note.StatusCode = noteData.StatusCode;
                    note.UpdatedAt = DateTime.UtcNow;
                    if (noteData.DeletedAt.HasValue)
                        note.DeletedAt = noteData.DeletedAt;
                    break;

                case 4: // File
                    var file = filesToUpdateDict[workspaceItem.EntityId];
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
