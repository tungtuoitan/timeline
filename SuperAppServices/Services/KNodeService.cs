using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Batch node operations (action-based) for k.node table.
    /// Pattern: Preload → Validate → Process → SaveChanges → SyncPaths
    /// </summary>
    public class KNodeService : IKNodeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KNodeService> _logger;
        private readonly IKNodeHelperService _helper;

        public KNodeService(
            ApplicationDbContext context,
            ILogger<KNodeService> logger,
            IKNodeHelperService helper)
        {
            _context = context;
            _logger  = logger;
            _helper  = helper;
        }

        public async Task<ResultOptions> UpsertNodesAsync(
            List<KUpsertNodeRequest> requests,
            int userId,
            int knowledgeId)
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
                        return new ResultOptions { Success = false, Message = "No nodes provided", Status = 400 };
                    }

                    _logger.LogInformation("Batch upsert {Count} nodes in knowledge {KnowledgeId}", requests.Count, knowledgeId);

                    // ===== STEP 1: PRELOAD existing nodes =====
                    var idsToLoad = requests
                        .Where(r => r.Action != KNodeAction.Create && r.Id.HasValue && r.Id.Value > 0)
                        .Select(r => r.Id!.Value)
                        .Distinct()
                        .ToList();

                    var existingNodes = await _context.KNodes
                        .IgnoreQueryFilters()
                        .Where(n => idsToLoad.Contains(n.Id))
                        .ToDictionaryAsync(n => n.Id);

                    // ===== STEP 2: VALIDATE =====
                    var errors = new List<string>();

                    foreach (var req in requests)
                    {
                        var err = _helper.ValidateRequest(req, existingNodes);
                        if (err != null) errors.Add(err);
                    }

                    foreach (var r in requests.Where(r => r.Action == KNodeAction.Move && r.Id.HasValue && r.ParentId.HasValue))
                    {
                        var circErr = await _helper.CheckCircularDependencyAsync(r.Id!.Value, r.ParentId!.Value, existingNodes);
                        if (circErr != null) errors.Add(circErr);
                    }

                    if (errors.Any())
                    {
                        await transaction.RollbackAsync();
                        return new ResultOptions { Success = false, Message = $"Validation failed: {string.Join("; ", errors)}", Status = 400 };
                    }

                    // ===== STEP 3: PROCESS =====
                    var upserted = new List<KNodeEntity>();

                    foreach (var req in requests)
                    {
                        switch (req.Action)
                        {
                            case KNodeAction.Create:
                                await _helper.ProcessCreateAsync(req, userId, knowledgeId, upserted);
                                break;
                            case KNodeAction.Update:
                                _helper.ProcessUpdate(req, existingNodes, upserted);
                                break;
                            case KNodeAction.Move:
                                _helper.ProcessMove(req, existingNodes, upserted);
                                break;
                            case KNodeAction.MoveCross:
                                await _helper.ProcessMoveCrossAsync(req, existingNodes, upserted);
                                break;
                            case KNodeAction.Delete:
                                _helper.ProcessDelete(req, existingNodes, upserted);
                                break;
                            case KNodeAction.Restore:
                                _helper.ProcessRestore(req, existingNodes, upserted);
                                break;
                            default:
                                throw new ArgumentException($"Unsupported action: {req.Action}");
                        }
                    }

                    // ===== STEP 4: SAVE =====
                    await _context.SaveChangesAsync();

                    // ===== STEP 5: SYNC PATHS =====
                    await _helper.SyncPathIdsAsync(upserted);

                    // ===== STEP 6: BUILD RESPONSE =====
                    var responseItems = upserted.Select(n => new
                    {
                        id          = n.Id,
                        knowledgeId = n.KnowledgeId,
                        parentId    = n.ParentId,
                        name        = n.Name,
                        description = n.Description,
                        color       = n.Color,
                        icon        = n.Icon,
                        pathIds     = n.PathIds,
                        pathDepth   = n.PathDepth,
                        createdAt   = n.CreatedAt,
                        updatedAt   = n.UpdatedAt,
                        deletedAt   = n.DeletedAt
                    }).Cast<object>().ToList();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Batch upsert committed — {Count} nodes in knowledge {KnowledgeId}", requests.Count, knowledgeId);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully processed {requests.Count} nodes",
                        Data    = responseItems,
                        Status  = 200
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency conflict during batch upsert nodes");
                    return new ResultOptions { Success = false, Message = "Concurrency conflict — all changes rolled back", Status = 409 };
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "DB update error during batch upsert nodes");
                    return new ResultOptions { Success = false, Message = $"Database update error — all changes rolled back. {ex.InnerException?.Message}", Status = 500 };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Unexpected error during batch upsert nodes");
                    return new ResultOptions { Success = false, Message = $"An error occurred — all changes rolled back. {ex.Message}", Status = 500 };
                }
            });
        }
    }
}
