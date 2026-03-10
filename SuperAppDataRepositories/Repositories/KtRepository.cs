using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class KtRepository : IKtRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KtRepository> _logger;

        public KtRepository(ApplicationDbContext context, ILogger<KtRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── Knowledge ────────────────────────────────────────────────────────

        public async Task<ResultOptions> GetKnowledgesAsync(KtKnowledgeFilterOptions filter)
        {
            try
            {
                var query = _context.KtKnowledges
                    .AsNoTracking()
                    .Where(k => k.UserId == filter.UserId);

                if (filter.RootsOnly)
                    query = query.Where(k => k.ParentId == null);
                else if (filter.ParentId.HasValue)
                    query = query.Where(k => k.ParentId == filter.ParentId);

                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                    query = query.Where(k => k.Title.Contains(filter.SearchText) || (k.Description != null && k.Description.Contains(filter.SearchText)));

                if (filter.DeletedAt == "null")
                    query = query.Where(k => k.DeletedAt == null);
                else if (filter.DeletedAt == "notNull")
                    query = query.Where(k => k.DeletedAt != null);

                var items = await query.OrderBy(k => k.ParentId).ThenBy(k => k.Title).ToListAsync();
                return new ResultOptions { Data = items.Cast<object>().ToList(), TotalCount = items.Count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetKnowledgesAsync failed for userId={UserId}", filter.UserId);
                return ResultOptions.Fail(ex.Message);
            }
        }

        public async Task<ResultOptions> UpsertKnowledgesAsync(List<KtKnowledge> items)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var updateIds = items.Where(x => x.Id > 0).Select(x => x.Id).ToList();
                    var existing = updateIds.Count > 0
                        ? await _context.KtKnowledges.Where(k => updateIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id)
                        : new Dictionary<int, KtKnowledge>();

                    var result = new List<KtKnowledge>();
                    foreach (var item in items)
                    {
                        if (item.Id > 0 && existing.TryGetValue(item.Id, out var current))
                        {
                            current.ParentId = item.ParentId;
                            current.Title = item.Title;
                            current.Description = item.Description;
                            current.DeletedAt = item.DeletedAt;
                            current.UpdatedAt = DateTime.UtcNow;
                            result.Add(current);
                        }
                        else
                        {
                            item.Id = 0;
                            item.CreatedAt = DateTime.UtcNow;
                            item.UpdatedAt = DateTime.UtcNow;
                            _context.KtKnowledges.Add(item);
                            result.Add(item);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    _logger.LogInformation("UpsertKnowledgesAsync: upserted {Count} items", result.Count);
                    return new ResultOptions { Data = result.Cast<object>().ToList() };
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "UpsertKnowledgesAsync failed");
                    return ResultOptions.Fail(ex.Message);
                }
            });
        }

        // ── Cards ─────────────────────────────────────────────────────────────

        public async Task<ResultOptions> GetCardsAsync(KtCardFilterOptions filter)
        {
            try
            {
                var query = _context.KtCards
                    .AsNoTracking()
                    .Include(c => c.SourceLinks)
                    .Where(c => c.UserId == filter.UserId);

                if (filter.KnowledgeId.HasValue)
                    query = query.Where(c => c.KnowledgeId == filter.KnowledgeId);

                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                    query = query.Where(c => c.Title.Contains(filter.SearchText) || c.Keyword.Contains(filter.SearchText) || (c.Description != null && c.Description.Contains(filter.SearchText)));

                if (filter.DeletedAt == "null")
                    query = query.Where(c => c.DeletedAt == null);
                else if (filter.DeletedAt == "notNull")
                    query = query.Where(c => c.DeletedAt != null);

                var cards = await query.OrderBy(c => c.KnowledgeId).ThenBy(c => c.Id).ToListAsync();
                return new ResultOptions { Data = cards.Cast<object>().ToList(), TotalCount = cards.Count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCardsAsync failed for userId={UserId}", filter.UserId);
                return ResultOptions.Fail(ex.Message);
            }
        }

        public async Task<ResultOptions> UpsertCardsAsync(List<KtCard> cards, Dictionary<int, List<int>> linkedCardIdsMap)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var updateIds = cards.Where(x => x.Id > 0).Select(x => x.Id).ToList();
                    var existing = updateIds.Count > 0
                        ? await _context.KtCards.Where(c => updateIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id)
                        : new Dictionary<int, KtCard>();

                    var savedCards = new List<KtCard>();
                    // track index để map với request's linkedCardIds
                    var indexMap = new List<(KtCard card, int requestIndex)>();

                    for (int i = 0; i < cards.Count; i++)
                    {
                        var card = cards[i];
                        if (card.Id > 0 && existing.TryGetValue(card.Id, out var current))
                        {
                            current.ParentCardId = card.ParentCardId;
                            current.Keyword = card.Keyword;
                            current.Title = card.Title;
                            current.Description = card.Description;
                            current.IsDefinition = card.IsDefinition;
                            current.DeletedAt = card.DeletedAt;
                            current.UpdatedAt = DateTime.UtcNow;
                            savedCards.Add(current);
                            indexMap.Add((current, i));
                        }
                        else
                        {
                            card.Id = 0;
                            card.CreatedAt = DateTime.UtcNow;
                            card.UpdatedAt = DateTime.UtcNow;
                            _context.KtCards.Add(card);
                            savedCards.Add(card);
                            indexMap.Add((card, i));
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Sync card links for relation cards
                    foreach (var (card, reqIdx) in indexMap)
                    {
                        if (card.IsDefinition) continue;
                        if (!linkedCardIdsMap.TryGetValue(reqIdx, out var targetIds)) continue;
                        if (card.DeletedAt != null) { targetIds = new List<int>(); }

                        // load existing links
                        var existingLinks = await _context.KtCardLinks
                            .Where(l => l.SourceCardId == card.Id)
                            .ToListAsync();

                        var toRemove = existingLinks.Where(l => !targetIds.Contains(l.TargetCardId)).ToList();
                        var toAdd = targetIds
                            .Where(tid => !existingLinks.Any(l => l.TargetCardId == tid))
                            .Select(tid => new KtCardLink { SourceCardId = card.Id, TargetCardId = tid })
                            .ToList();

                        if (toRemove.Count > 0) _context.KtCardLinks.RemoveRange(toRemove);
                        if (toAdd.Count > 0) _context.KtCardLinks.AddRange(toAdd);
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    _logger.LogInformation("UpsertCardsAsync: upserted {Count} cards", savedCards.Count);
                    return new ResultOptions { Data = savedCards.Cast<object>().ToList() };
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "UpsertCardsAsync failed");
                    return ResultOptions.Fail(ex.Message);
                }
            });
        }
    }
}
