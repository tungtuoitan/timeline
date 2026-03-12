using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SuperAppModels.Models;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppDataRepositories.Data;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Batch workspace item operations (action-based).
    /// kws.workspace_items is a self-contained node table.
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

        public async Task<ResultOptions> UpsertWorkspaceItemsAsync(
            List<KUpsertWorkspaceItemRequest> requests,
            int userId,
            int workspaceId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (requests == null || !requests.Any())
                    {
                        await transaction.RollbackAsync();
                        return new ResultOptions { Success = false, Message = "No workspace items provided", Status = 400 };
                    }

                    _logger.LogInformation("Batch upsert {Count} items in workspace {WorkspaceId}", requests.Count, workspaceId);

                    // ===== STEP 1: PRELOAD existing items =====
                    var itemIdsToLoad = requests
                        .Where(r => r.Action != KWorkspaceItemAction.Create && r.Id.HasValue && r.Id.Value > 0)
                        .Select(r => r.Id!.Value)
                        .Distinct()
                        .ToList();

                    var existingItemsDict = await _context.KWorkspaceItems
                        .IgnoreQueryFilters()
                        .Where(wi => itemIdsToLoad.Contains(wi.Id))
                        .ToDictionaryAsync(wi => wi.Id);

                    // ===== STEP 2: VALIDATE =====
                    var validationErrors = new List<string>();

                    foreach (var request in requests)
                    {
                        var error = _helperService.ValidateRequest(request, existingItemsDict);
                        if (error != null)
                            validationErrors.Add(error);
                    }

                    // Circular dependency check for Move
                    foreach (var r in requests.Where(r => r.Action == KWorkspaceItemAction.Move && r.Id.HasValue && r.ParentId.HasValue))
                    {
                        var circularError = await _helperService.CheckCircularDependencyAsync(r.Id!.Value, r.ParentId!.Value, existingItemsDict);
                        if (circularError != null)
                            validationErrors.Add(circularError);
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

                    // ===== STEP 3: PROCESS =====
                    var upsertedItems = new List<KWorkspaceItemEntity>();

                    foreach (var request in requests)
                    {
                        switch (request.Action)
                        {
                            case KWorkspaceItemAction.Create:
                                await _helperService.ProcessCreateActionAsync(request, userId, workspaceId, upsertedItems);
                                break;

                            case KWorkspaceItemAction.Update:
                                _helperService.ProcessUpdateNodeAction(request, existingItemsDict, upsertedItems);
                                break;

                            case KWorkspaceItemAction.Move:
                                _helperService.ProcessMoveAction(request, existingItemsDict, upsertedItems);
                                break;

                            case KWorkspaceItemAction.MoveCross:
                                await _helperService.ProcessMoveCrossActionAsync(request, existingItemsDict, upsertedItems);
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

                    // ===== STEP 4: SAVE =====
                    await _context.SaveChangesAsync();

                    // ===== STEP 5: SYNC PATH =====
                    await _helperService.SyncPathIdsAsync(upsertedItems);

                    // ===== STEP 6: BUILD RESPONSE =====
                    var responseItems = upsertedItems.Select(item => new
                    {
                        id = item.Id,
                        workspaceId = item.WorkspaceId,
                        parentId = item.ParentId,
                        name = item.Name,
                        description = item.Description,
                        color = item.Color,
                        icon = item.Icon,
                        pathIds = item.PathIds,
                        pathDepth = item.PathDepth,
                        createdAt = item.CreatedAt,
                        updatedAt = item.UpdatedAt,
                        deletedAt = item.DeletedAt
                    }).Cast<object>().ToList();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Batch upsert committed for {Count} items in workspace {WorkspaceId}", requests.Count, workspaceId);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully processed {requests.Count} workspace items",
                        Data = responseItems,
                        Status = 200
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency conflict during batch upsert workspace items");
                    return new ResultOptions { Success = false, Message = "Concurrency conflict - all changes rolled back", Status = 409 };
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database update error during batch upsert workspace items");
                    return new ResultOptions { Success = false, Message = $"Database update error - all changes rolled back. {ex.InnerException?.Message}", Status = 500 };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Unexpected error during batch upsert workspace items");
                    return new ResultOptions { Success = false, Message = $"An error occurred - all changes rolled back. {ex.Message}", Status = 500 };
                }
            });
        }
    }
}
