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
    public class KWorkspaceItemService : IKWorkspaceItemService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KWorkspaceItemService> _logger;
        private readonly IKWorkspaceItemHelperService _helperService;

        public KWorkspaceItemService(
            ApplicationDbContext context,
            ILogger<KWorkspaceItemService> logger,
            IKWorkspaceItemHelperService helperService)
        {
            _context = context;
            _logger = logger;
            _helperService = helperService;
        }

        /// <summary>
        /// Batch upsert workspace items with transaction management and action-based validation
        /// Pattern: Preload → Validate → Process → Single SaveChanges
        /// </summary>
        public async Task<ResultOptions> UpsertWorkspaceItemsAsync(
            List<KUpsertWorkspaceItemRequest> requests,
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







                    // ===== STEP 2: PRELOAD DATA =====

                    // Preload workspace items for UpdateFolder/Move/Delete/Restore actions
                    var itemIdsToUpdate = requests
                        .Where(r => r.Action == KWorkspaceItemAction.Move ||
                                   r.Action == KWorkspaceItemAction.MoveCross ||
                                   r.Action == KWorkspaceItemAction.UpdateFolder ||
                                   r.Action == KWorkspaceItemAction.Delete ||
                                   r.Action == KWorkspaceItemAction.Restore)
                        .Where(r => r.Id.HasValue && r.Id.Value > 0)
                        .Select(r => r.Id.Value)
                        .Distinct()
                        .ToList();

                    var existingItemsDict = await _context.KWorkspaceItems
                        .IgnoreQueryFilters()
                        .Where(wi => itemIdsToUpdate.Contains(wi.Id))
                        .ToDictionaryAsync(wi => wi.Id, wi => wi);

                    // Preload entity IDs for Add action (verify existing entities)
                    var folderIdsToAdd = requests
                        .Where(r => r.Action == KWorkspaceItemAction.Add && r.EntityType == 2 && r.EntityId.HasValue)
                        .Select(r => r.EntityId.Value)
                        .Distinct()
                        .ToList();

                    var noteIdsToAdd = requests
                        .Where(r => r.Action == KWorkspaceItemAction.Add && r.EntityType == 3 && r.EntityId.HasValue)
                        .Select(r => r.EntityId.Value)
                        .Distinct()
                        .ToList();

                    var fileIdsToAdd = requests
                        .Where(r => r.Action == KWorkspaceItemAction.Add && r.EntityType == 4 && r.EntityId.HasValue)
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

                    // Preload folders for UpdateFolder action (FOLDER ONLY)
                    var folderIdsToUpdate = requests
                        .Where(r => r.Action == KWorkspaceItemAction.UpdateFolder && r.Id.HasValue)
                        .Select(r => r.Id.Value)
                        .Where(id => existingItemsDict.ContainsKey(id) && existingItemsDict[id].EntityType == 2)
                        .Select(id => existingItemsDict[id].EntityId)
                        .Distinct()
                        .ToList();

                    var foldersToUpdateDict = folderIdsToUpdate.Any()
                        ? await _context.Folders.Where(f => folderIdsToUpdate.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f)
                        : new Dictionary<int, Folder>();

                    // Note: Notes and Files use their own entity-specific APIs for updates
                    var notesToUpdateDict = new Dictionary<int, Note>();
                    var filesToUpdateDict = new Dictionary<int, SuperAppModels.Models.File>();











                    // ===== STEP 3: VALIDATE =====

                    var validationErrors = new List<string>();

                    foreach (var request in requests)
                    {
                        var validationResult = _helperService.ValidateRequest(
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
                    var moveRequests = requests.Where(r => r.Action == KWorkspaceItemAction.Move).ToList();
                    foreach (var moveRequest in moveRequests)
                    {
                        if (moveRequest.Id.HasValue && moveRequest.ParentId.HasValue)
                        {
                            var circularCheck = await _helperService.CheckCircularDependencyAsync(
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















                    // ===== STEP 4: PROCESS REQUESTS =====

                    var upsertedItems = new List<KWorkspaceItemEntity>();

                    foreach (var request in requests)
                    {
                        switch (request.Action)
                        {
                            case KWorkspaceItemAction.Create:
                                await _helperService.ProcessCreateActionAsync(request, userId, workspaceId, upsertedItems);
                                break;

                            case KWorkspaceItemAction.Add:
                                _helperService.ProcessAddAction(request, workspaceId, upsertedItems);
                                break;

                            case KWorkspaceItemAction.Move:
                                _helperService.ProcessMoveAction(request, existingItemsDict, upsertedItems);
                                break;

                            case KWorkspaceItemAction.MoveCross:
                                await _helperService.ProcessMoveCrossActionAsync(request, existingItemsDict, upsertedItems);
                                break;

                            case KWorkspaceItemAction.UpdateFolder:
                                await _helperService.ProcessUpdateFolderActionAsync(
                                    request,
                                    existingItemsDict,
                                    foldersToUpdateDict);
                                break;

                            case KWorkspaceItemAction.Delete:
                                _helperService.ProcessDeleteAction(request, existingItemsDict, upsertedItems);
                                break;

                            case KWorkspaceItemAction.Restore:
                                _helperService.ProcessRestoreAction(request, existingItemsDict, upsertedItems);
                                break;

                            default:
                                throw new ArgumentException($"Unsupported action: {request.Action}");
                        }
                    }










                    // ===== STEP 5: SINGLE SAVECHANGES (Transaction Atomicity!) =====
                    await _context.SaveChangesAsync();


                    // ===== STEP 5.1: SYNC KEYWORDS =====
                    await _helperService.SyncPathIdsAndKeywordsAsync(requests, upsertedItems, userId);








                    // ===== STEP 5.5: RESPONSE   =====
                    var responseItems = new List<object>();

                    foreach (var item in upsertedItems)
                    {
                        object? entityData = null;

                        switch (item.EntityType)
                        {
                            case 2: // Folder
                                entityData = await _context.Folders
                                    .AsNoTracking()
                                    .Where(f => f.Id == item.EntityId)
                                    .Select(f => new
                                    {
                                        id = f.Id,
                                        userId = f.UserId,
                                        name = f.Name,
                                        description = f.Description,
                                        color = f.Color,
                                        icon = f.Icon,
                                        createdAt = f.CreatedAt,
                                        updatedAt = f.UpdatedAt,
                                        deletedAt = f.DeletedAt
                                    })
                                    .FirstOrDefaultAsync();
                                break;

                            case 3: // Note
                                entityData = await _context.Notes
                                    .AsNoTracking()
                                    .Where(n => n.Id == item.EntityId)
                                    .Select(n => new
                                    {
                                        id = n.Id,
                                        userId = n.UserId,
                                        name = n.Name,
                                        description = n.Description,
                                        statusCode = n.StatusCode,
                                        createdAt = n.CreatedAt,
                                        updatedAt = n.UpdatedAt,
                                        deletedAt = n.DeletedAt
                                    })
                                    .FirstOrDefaultAsync();
                                break;

                            case 4: // File
                                entityData = await _context.Files
                                    .AsNoTracking()
                                    .Where(f => f.Id == item.EntityId)
                                    .Select(f => new
                                    {
                                        id = f.Id,
                                        userId = f.UserId,
                                        name = f.Name,
                                        url = f.Url,
                                        fileSize = f.FileSize,
                                        mimeType = f.MimeType,
                                        extension = f.Extension,
                                        statusCode = f.StatusCode,
                                        createdAt = f.CreatedAt,
                                        updatedAt = f.UpdatedAt,
                                        deletedAt = f.DeletedAt
                                    })
                                    .FirstOrDefaultAsync();
                                break;
                        }

                        // Return WorkspaceItemV2-like structure with full entity data
                        responseItems.Add(new
                        {
                            id = item.Id,
                            workspaceId = item.WorkspaceId,
                            parentId = item.ParentId,
                            entityType = item.EntityType,
                            entityId = item.EntityId,
                            createdAt = item.CreatedAt,
                            updatedAt = item.UpdatedAt,
                            deletedAt = item.DeletedAt,
                            data = entityData // ← Full entity data (Folder/Note/File)
                        });
                    }









                    // ===== STEP 6: COMMIT =====
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Successfully committed batch upsert transaction for {Count} workspace items",
                        requests.Count);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully processed {requests.Count} workspace items in batch",
                        Data = responseItems.Cast<object>().ToList(), // ← WITH entity data
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
    }
}
