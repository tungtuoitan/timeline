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
    /// Handles 7 explicit actions: Create, Add, Move, MoveCross, UpdateFolder, Delete, Restore
    /// Pattern: 100% follows NoteRepository.UpsertNotesAsync (preload, validate, process, single save)
    /// Note: UpdateFolder only updates folder entities. Notes/Files use their own entity-specific APIs.
    /// </summary>
    public class WorkspaceItemHelperService: IWorkspaceItemHelperService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkspaceItemService> _logger;
        private readonly KeywordServiceV2 _keywordService;

        public WorkspaceItemHelperService(
            ApplicationDbContext context,
            ILogger<WorkspaceItemService> logger,
            WorkspaceItemPathService pathService,
            KeywordServiceV2 keywordService)
        {
            _context = context;
            _logger = logger;
            _keywordService = keywordService;
        }


        // =====================================================================
        // VALIDATION METHODS
        // =====================================================================

        /// <summary>
        /// Validate request based on action type
        /// Returns error message if invalid, null if valid
        /// </summary>
        public string? ValidateRequest(
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

                case WorkspaceItemAction.UpdateFolder:
                    // Required: Id, FolderData
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "UpdateFolder action requires valid Id";

                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";

                    var workspaceItem = existingItemsDict[request.Id.Value];

                    // ONLY support Folder (EntityType = 2)
                    if (workspaceItem.EntityType != 2)
                        return $"UpdateFolder action only supports folders (EntityType=2), but got EntityType={workspaceItem.EntityType}";

                    // Validate folder exists
                    if (!foldersToUpdateDict.ContainsKey(workspaceItem.EntityId))
                        return $"Folder with EntityId={workspaceItem.EntityId} not found for update";

                    // Validate FolderData is provided
                    if (request.FolderData == null)
                        return "UpdateFolder action requires FolderData";

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
        public async Task<string?> CheckCircularDependencyAsync(
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
        public bool HasEntityData(UpsertWorkspaceItemRequest request, byte? entityType = null)
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
        public async Task ProcessCreateActionAsync(
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
                        Icon = noteData.Icon,
                        Color = noteData.Color,
                        CreatedAt = DateTime.UtcNow,
                        DeletedAt = null
                    };

                    // Process external links in description: [[name|url]] → [[id]]
                    if (!string.IsNullOrEmpty(newNote.Description))
                    {
                        try
                        {
                            newNote.Description = await _keywordService.ProcessExternalLinksInDescriptionAsync(
                                newNote.Description,
                                userId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing external links for new note with Name: {Name}", newNote.Name);
                            throw; // Re-throw to fail the transaction
                        }
                    }

                    _context.Notes.Add(newNote);
                    await _context.SaveChangesAsync();
                    entityId = newNote.Id;
                    
                    // trước khi note mới được tạo ra, thì chưa có chỗ nào reference đến chính nó cả. nên ta chỉ cần update noteId là xong
                    // Replace negative noteIds in wiki links with real noteId
                    if (!string.IsNullOrEmpty(newNote.Description) && newNote.Description.Contains("[["))
                    {
                        var regex = new System.Text.RegularExpressions.Regex(@"\[\[(-\d+)/([^\]]+)\]\]");
                        var updatedDescription = regex.Replace(newNote.Description, $"[[{newNote.Id}/$2]]");
                        
                        if (updatedDescription != newNote.Description)
                        {
                            _logger.LogInformation(
                                "Replacing negative noteIds in description for newly created note ID: {NoteId}",
                                newNote.Id);
                            
                            newNote.Description = updatedDescription;
                            newNote.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                DeletedAt = null
            };

            _context.WorkspaceItems.Add(newItem);
            upsertedItems.Add(newItem);

            // Save to get workspace_item ID
            await _context.SaveChangesAsync();

            // Sync keywords for note (EntityType=3)
            if (request.EntityType.Value == 3)
            {
                _logger.LogInformation(
                    "Syncing keywords for newly created note workspace_item ID {WorkspaceItemId}",
                    newItem.Id);

                try
                {
                    await _keywordService.SyncNoteKeywordAsync(newItem.Id, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing keywords for note workspace_item {WorkspaceItemId}", newItem.Id);
                    // Don't throw - keyword sync failure shouldn't fail the whole operation
                }
            }

            _logger.LogInformation(
                "Created entity ID {EntityId} and workspace_item for EntityType {EntityType}",
                entityId, request.EntityType);
        }

        /// <summary>
        /// ADD: Add existing entity to workspace
        /// </summary>
        public void ProcessAddAction(
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
        public void ProcessMoveAction(
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
        public async Task ProcessMoveCrossActionAsync(
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
        public async Task<List<WorkspaceItemEntity>> GetAllDescendantsAsync(int parentWorkspaceItemId)
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
        /// UPDATE FOLDER: Update folder data (FOLDER ONLY)
        /// Notes and Files use their own entity-specific APIs for updates
        /// </summary>
        public async Task ProcessUpdateFolderActionAsync(
            UpsertWorkspaceItemRequest request,
            Dictionary<int, WorkspaceItemEntity> existingItemsDict,
            Dictionary<int, Folder> foldersToUpdateDict)
        {
            _logger.LogInformation(
                "Processing UPDATE_FOLDER action: Id={Id}",
                request.Id);

            var workspaceItem = existingItemsDict[request.Id!.Value];

            // Update folder entity
            var folder = foldersToUpdateDict[workspaceItem.EntityId];
            var folderData = request.FolderData!;

            folder.Name = folderData.Name;
            folder.Description = folderData.Description;
            folder.Color = folderData.Color;
            folder.Icon = folderData.Icon;
            folder.UpdatedAt = DateTime.UtcNow;

            if (folderData.DeletedAt.HasValue)
                folder.DeletedAt = folderData.DeletedAt;

            // Sync keywords to update folder name in Keywords table
            _logger.LogInformation(
                "Syncing keywords for updated folder workspace_item ID {WorkspaceItemId}",
                request.Id);

            try
            {
                // Get userId from folder
                await _keywordService.SyncFolderKeywordAsync(workspaceItem.Id, folder.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing keywords for folder workspace_item {WorkspaceItemId}", workspaceItem.Id);
                // Don't throw - keyword sync failure shouldn't fail the whole operation
            }

            _logger.LogInformation(
                "Updated folder (EntityId={EntityId}) for workspace_item ID {WorkspaceItemId}",
                workspaceItem.EntityId, request.Id);
        }

        /// <summary>
        /// DELETE: Soft delete workspace_item
        /// </summary>
        public void ProcessDeleteAction(
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
        public void ProcessRestoreAction(
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

        /// <summary>
        /// Sync PathIds and Keywords after Create/Move/Rename operations
        /// Also handles hard deleting keywords for deleted items
        /// </summary>
        public async Task SyncPathIdsAndKeywordsAsync(
            List<UpsertWorkspaceItemRequest> requests,
            List<WorkspaceItemEntity> upsertedItems,
            int userId)
        {
            _logger.LogInformation("=== STEP 5.1: Syncing PathIds and Keywords for {Count} workspace items ===", upsertedItems.Count);

            // ===== STEP 1: Rebuild PathIds for ALL items FIRST =====
            _logger.LogInformation("STEP 1: Rebuilding PathIds for all items...");
            foreach (var item in upsertedItems)
            {
                try
                {
                    // Check if item is deleted (soft delete)
                    var isDeleted = item.DeletedAt.HasValue;
                    
                    if (!isDeleted)
                    {
                        _logger.LogInformation("Rebuilding PathIds for item {ItemId} (EntityType: {EntityType}, ParentId: {ParentId}, Current PathIds: '{PathIds}')",
                            item.Id, item.EntityType, item.ParentId, item.PathIds);
                        
                        // Rebuild PathIds if not set correctly
                        await RebuildPathIdsAsync(item, userId);
                        
                        _logger.LogInformation("After rebuild - Item {ItemId} PathIds: '{PathIds}', PathDepth: {PathDepth}",
                            item.Id, item.PathIds, item.PathDepth);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rebuild PathIds for workspace item {Id}", item.Id);
                    // Continue processing other items
                }
            }

            // Save PathIds changes
            _logger.LogInformation("Saving PathIds changes to database...");
            await _context.SaveChangesAsync();
            _logger.LogInformation("PathIds saved successfully.");

            // ===== STEP 2: Sync Keywords AFTER PathIds are set =====
            _logger.LogInformation("STEP 2: Syncing Keywords after PathIds are set...");
            // Collect deleted note IDs for hard deleting keywords
            var deletedNoteIds = new List<int>();

            foreach (var item in upsertedItems)
            {
                try
                {
                    // Check if item is deleted (soft delete)
                    var isDeleted = item.DeletedAt.HasValue;

                    if (isDeleted && item.EntityType == 3) // Note
                    {
                        // Collect note IDs for hard deleting keywords
                        deletedNoteIds.Add(item.EntityId);
                        continue; // Skip keyword sync for deleted items
                    }

                    if (isDeleted)
                    {
                        // Skip keyword sync for deleted items (non-notes)
                        continue;
                    }

                    // Sync keywords based on entity type
                    switch (item.EntityType)
                    {
                        case 1: // Workspace
                            // Workspaces don't have workspace_items representation
                            break;

                        case 2: // Folder
                            var folder = await _context.Folders.FindAsync(item.EntityId);
                            if (folder != null && !folder.DeletedAt.HasValue)
                            {
                                await _keywordService.SyncFolderKeywordAsync(item.Id, userId);
                            }
                            break;

                        case 3: // Note
                            var note = await _context.Notes.FindAsync(item.EntityId);
                            if (note != null && !note.DeletedAt.HasValue)
                            {
                                await _keywordService.SyncNoteKeywordAsync(item.Id, userId);
                            }
                            break;

                        case 4: // File
                            // TODO: Implement file keyword sync if needed
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to sync Keywords for workspace item {Id}", item.Id);
                    // Continue processing other items
                }
            }

            // Hard delete keywords for deleted notes
            if (deletedNoteIds.Any())
            {
                try
                {
                    await _keywordService.HardDeleteNoteKeywordsAsync(deletedNoteIds);
                    _logger.LogInformation("Hard deleted keywords for {Count} deleted notes", deletedNoteIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error hard deleting keywords for deleted notes");
                    // Don't throw - keyword cleanup can be done later
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Rebuild PathIds for a workspace item based on its parent
        /// </summary>
        public async Task RebuildPathIdsAsync(WorkspaceItemEntity item, int userId)
        {
            string newPathIds;
            int newDepth;

            if (item.ParentId.HasValue)
            {
                var parent = await _context.Set<WorkspaceItemEntity>().FindAsync(item.ParentId.Value);
                if (parent == null)
                {
                    _logger.LogWarning("Parent {ParentId} not found for item {ItemId}",
                        item.ParentId.Value, item.Id);
                    return;
                }

                // Child item: append to parent's path
                // Example: parent=/175/ (depth=1) → child=/175/174/ (depth=2)
                newPathIds = $"{parent.PathIds}{item.Id}/";
                newDepth = parent.PathDepth + 1;
            }
            else
            {
                // Root folder in workspace (ParentId=NULL): depth = 1
                // Workspace is considered depth=0 (not in workspace_items)
                // Example: /175/ (root folder - first level child of workspace)
                // NOTE: PathIds does NOT contain workspaceId (workspace is not in workspace_items)
                newPathIds = $"/{item.Id}/";
                newDepth = 1;
            }

            // Update if changed
            if (item.PathIds != newPathIds || item.PathDepth != newDepth)
            {
                var oldPathIds = item.PathIds;
                item.PathIds = newPathIds;
                item.PathDepth = newDepth;

                _logger.LogInformation("Updated PathIds for item {Id}: {OldPath} → {NewPath}",
                    item.Id, oldPathIds, newPathIds);

                // If item was moved, update descendants
                if (!string.IsNullOrEmpty(oldPathIds) && oldPathIds != "/" && oldPathIds != newPathIds)
                {
                    await UpdateDescendantPathIdsAsync(item.Id, oldPathIds, newPathIds, userId);
                }
            }
        }

        /// <summary>
        /// Update PathIds for all descendants when parent moves
        /// </summary>
        public async Task UpdateDescendantPathIdsAsync(int parentId, string oldPathIds, string newPathIds, int userId)
        {
            var descendants = await _context.Set<WorkspaceItemEntity>()
                .Where(i => i.PathIds.StartsWith(oldPathIds) && i.Id != parentId)
                .ToListAsync();

            _logger.LogInformation("Updating PathIds for {Count} descendants of item {ParentId}",
                descendants.Count, parentId);

            foreach (var descendant in descendants)
            {
                descendant.PathIds = descendant.PathIds.Replace(oldPathIds, newPathIds);

                // Calculate PathDepth from PathIds
                // NOTE: PathIds does NOT contain workspaceId (workspace is not in workspace_items)
                // Workspace is considered depth=0 (root), folders start at depth=1
                // Formula: PathDepth = Count('/') - 1
                // Examples:
                //   /175/          → 2 slashes → 2 - 1 = 1 (root folder - child of workspace)
                //   /175/174/      → 3 slashes → 3 - 1 = 2 (depth 2 - nested folder)
                //   /175/174/180/  → 4 slashes → 4 - 1 = 3 (depth 3 - nested folder)
                //   /a/b/c/d/e/    → 6 slashes → 6 - 1 = 5 (depth 5 - deeply nested)
                descendant.PathDepth = descendant.PathIds.Count(c => c == '/') - 1;
            }

            // Sync keywords for descendants after move
            // Note: Keywords are now found by TargetItemId, so they'll be updated, not duplicated
            foreach (var descendant in descendants)
            {
                try
                {
                    if (descendant.EntityType == 2) // Folder
                    {
                        await _keywordService.SyncFolderKeywordAsync(descendant.Id, userId);
                    }
                    else if (descendant.EntityType == 3) // Note
                    {
                        await _keywordService.SyncNoteKeywordAsync(descendant.Id, userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing keyword for descendant {ItemId} after move", descendant.Id);
                    // Continue with other descendants
                }
            }

            if (descendants.Any())
            {
                _logger.LogInformation("Synced keywords for {Count} descendants after move", descendants.Count);
            }
        }
    }
}
