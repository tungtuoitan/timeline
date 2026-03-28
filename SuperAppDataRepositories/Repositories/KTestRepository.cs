using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class KTestRepository : IKTestRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KTestRepository> _logger;

        public KTestRepository(ApplicationDbContext context, ILogger<KTestRepository> logger)
        {
            _context = context;
            _logger  = logger;
        }

        // ── List tests with last submission stats ────────────────────────────

        public async Task<List<KTestSummaryResponse>> GetTestSummariesAsync(int knowledgeId, int userId)
        {
            try
            {
                var tests = await _context.KTests
                    .Where(t => t.KnowledgeId == knowledgeId && t.DeletedAt == null)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                if (!tests.Any()) return [];

                var testIds = tests.Select(t => t.Id).ToList();

                // Node counts from k.test_node
                var nodeCounts = await _context.KTestNodes
                    .Where(tn => testIds.Contains(tn.TestId))
                    .GroupBy(tn => tn.TestId)
                    .Select(g => new { TestId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TestId, x => x.Count);

                // Active node counts (IsActive = true)
                var activeCounts = await _context.KTestNodes
                    .Where(tn => testIds.Contains(tn.TestId) && tn.IsActive)
                    .GroupBy(tn => tn.TestId)
                    .Select(g => new { TestId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TestId, x => x.Count);

                // Latest point history per (test, node) — for last-submission score
                var latestRows = await _context.KPointHistory
                    .Where(p => testIds.Contains(p.TestId) && p.UserId == userId)
                    .GroupBy(p => new { p.TestId, p.NodeId })
                    .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                    .ToListAsync();

                // All history rows for sparkline — group by (TestId, second-truncated timestamp)
                // to identify individual submissions, then compute pct per submission
                var allHistory = await _context.KPointHistory
                    .Where(p => testIds.Contains(p.TestId) && p.UserId == userId)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

                // Build scoreHistory per testId: last 10 submission pct values oldest→newest
                var scoreHistoryByTest = allHistory
                    .GroupBy(p => p.TestId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            // Group rows into discrete submissions by truncating to second
                            .GroupBy(p => new DateTime(
                                p.CreatedAt.Year, p.CreatedAt.Month, p.CreatedAt.Day,
                                p.CreatedAt.Hour, p.CreatedAt.Minute, p.CreatedAt.Second))
                            .OrderBy(sg => sg.Key)
                            .Select(sg =>
                            {
                                var total   = sg.Sum(x => x.Point);
                                var maxPts  = sg.Count() * 5;
                                return maxPts > 0 ? (int)Math.Round((double)total / maxPts * 100) : 0;
                            })
                            .TakeLast(10)
                            .ToList()
                    );

                return tests.Select(t =>
                {
                    var rows        = latestRows.Where(r => r.TestId == t.Id).ToList();
                    var nodeCount   = nodeCounts.TryGetValue(t.Id, out var c) ? c : 0;
                    var activeCount = activeCounts.TryGetValue(t.Id, out var ac) ? ac : 0;
                    int? total    = rows.Any() ? rows.Sum(r => r.Point) : null;
                    int? max      = rows.Any() ? nodeCount * 5 : null;
                    int? pct      = total.HasValue && max > 0
                        ? (int)Math.Round((double)total.Value / max.Value * 100) : null;

                    return new KTestSummaryResponse
                    {
                        Id              = t.Id,
                        KnowledgeId     = t.KnowledgeId,
                        UserId          = t.UserId,
                        Title           = t.Title,
                        Level           = t.Level,
                        Mode            = t.Mode,
                        NodeCount       = nodeCount,
                        ActiveCount     = activeCount,
                        LastTotalPoints = total,
                        LastMaxPoints   = max,
                        LastPct         = pct,
                        LastSubmittedAt = rows.Any() ? rows.Max(r => r.CreatedAt) : null,
                        CreatedAt       = t.CreatedAt,
                        ScoreHistory    = scoreHistoryByTest.TryGetValue(t.Id, out var hist) ? hist : [],
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test summaries for knowledge {KnowledgeId}, user {UserId}", knowledgeId, userId);
                throw;
            }
        }

        // ── Get test by ID (with nodes) ──────────────────────────────────────

        public async Task<KTestEntity?> GetTestByIdAsync(int testId, int knowledgeId)
        {
            try
            {
                return await _context.KTests
                    .Include(t => t.TestNodes)
                    .FirstOrDefaultAsync(t => t.Id == testId && t.KnowledgeId == knowledgeId && t.DeletedAt == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test ID {TestId} for knowledge {KnowledgeId}", testId, knowledgeId);
                throw;
            }
        }

        // ── Create test + test_node rows ─────────────────────────────────────

        public async Task<KTestEntity> CreateTestAsync(KTestEntity test, List<int> nodeIds)
        {
            try
            {
                _context.KTests.Add(test);
                await _context.SaveChangesAsync();

                if (nodeIds.Any())
                {
                    var testNodes = nodeIds.Select(nodeId => new KTestNodeEntity
                    {
                        TestId = test.Id,
                        NodeId = nodeId,
                    }).ToList();

                    _context.KTestNodes.AddRange(testNodes);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created test ID {Id} with {Count} nodes", test.Id, nodeIds.Count);
                return test;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test for knowledge {KnowledgeId}", test.KnowledgeId);
                throw;
            }
        }

        // ── Get question nodes ───────────────────────────────────────────────

        public async Task<List<KNodeEntity>> GetQuestionNodesAsync(
            int knowledgeId,
            List<int> entityNodeIds,
            bool includeDescendants)
        {
            try
            {
                IQueryable<KNodeEntity> query = _context.KNodes
                    .Where(n => n.KnowledgeId == knowledgeId
                             && n.DeletedAt == null
                             && n.NodeType == "question");

                if (!includeDescendants)
                {
                    query = query.Where(n => entityNodeIds.Contains(n.ParentId ?? -1));
                }
                else
                {
                    var pathPatterns = entityNodeIds.Select(id => $"/{id}/").ToList();
                    query = query.Where(n => pathPatterns.Any(p => n.PathIds.Contains(p))
                                          || entityNodeIds.Contains(n.ParentId ?? -1));
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question nodes for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        // ── Get question nodes by IDs ────────────────────────────────────────

        public async Task<List<KNodeEntity>> GetQuestionNodesByIdsAsync(List<int> nodeIds)
        {
            try
            {
                if (!nodeIds.Any()) return [];
                return await _context.KNodes
                    .Where(n => nodeIds.Contains(n.Id) && n.DeletedAt == null)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question nodes by IDs (count={Count})", nodeIds.Count);
                throw;
            }
        }

        // ── Per-node score history for a test ───────────────────────────────────

        public async Task<List<KPointHistoryEntity>> GetHistoryForNodesAsync(int testId, int userId, List<int> nodeIds)
        {
            try
            {
                return await _context.KPointHistory
                    .Where(p => p.TestId == testId && p.UserId == userId
                                && p.NodeId != null && nodeIds.Contains(p.NodeId.Value))
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node history for test {TestId}", testId);
                return [];
            }
        }

        // ── Save submission ──────────────────────────────────────────────────

        public async Task SaveSubmissionAsync(
            int testId,
            int userId,
            List<(int NodeId, string? AnswerText, int Point)> results)
        {
            try
            {
                var rows = results.Select(r => new KPointHistoryEntity
                {
                    TestId     = testId,
                    UserId     = userId,
                    NodeId     = r.NodeId,
                    AnswerText = r.AnswerText,
                    Point      = r.Point,
                    CreatedAt  = DateTime.UtcNow,
                }).ToList();

                _context.KPointHistory.AddRange(rows);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved {Count} point history rows for test {TestId}", rows.Count, testId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving submission for test {TestId}, user {UserId}", testId, userId);
                throw;
            }
        }

        // ── Get latest result for a test ─────────────────────────────────────

        public async Task<List<KPointHistoryEntity>> GetLatestResultAsync(int testId, int userId)
        {
            try
            {
                return await _context.KPointHistory
                    .Where(p => p.TestId == testId && p.UserId == userId)
                    .GroupBy(p => p.NodeId)
                    .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest result for test {TestId}, user {UserId}", testId, userId);
                throw;
            }
        }

        // ── Node scores (latest point per node, across all tests) ────────────

        public async Task<Dictionary<int, int>> GetNodeScoresAsync(int knowledgeId, int userId)
        {
            try
            {
                var testIds = await _context.KTests
                    .Where(t => t.KnowledgeId == knowledgeId && t.DeletedAt == null)
                    .Select(t => t.Id)
                    .ToListAsync();

                if (!testIds.Any()) return [];

                var rows = await _context.KPointHistory
                    .Where(p => testIds.Contains(p.TestId) && p.UserId == userId && p.NodeId.HasValue)
                    .GroupBy(p => p.NodeId!.Value)
                    .Select(g => new { NodeId = g.Key, Point = g.OrderByDescending(p => p.CreatedAt).First().Point })
                    .ToListAsync();

                return rows.ToDictionary(r => r.NodeId, r => r.Point);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node scores for knowledge {KnowledgeId}, user {UserId}", knowledgeId, userId);
                throw;
            }
        }

        // ── Update test title ────────────────────────────────────────────────

        public async Task<KTestEntity?> UpdateTestTitleAsync(int testId, int knowledgeId, string title)
        {
            try
            {
                var test = await _context.KTests
                    .FirstOrDefaultAsync(t => t.Id == testId && t.KnowledgeId == knowledgeId && t.DeletedAt == null);
                if (test == null) return null;

                test.Title     = title;
                test.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return test;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test title for {TestId}", testId);
                throw;
            }
        }

        // ── Add new test nodes ────────────────────────────────────────────────

        public async Task AddTestNodesAsync(int testId, List<int> nodeIds)
        {
            try
            {
                // Avoid duplicate entries
                var existing = await _context.KTestNodes
                    .Where(tn => tn.TestId == testId && nodeIds.Contains(tn.NodeId))
                    .Select(tn => tn.NodeId)
                    .ToListAsync();

                var toAdd = nodeIds
                    .Except(existing)
                    .Select(nodeId => new KTestNodeEntity { TestId = testId, NodeId = nodeId, IsActive = true })
                    .ToList();

                if (toAdd.Count > 0)
                {
                    _context.KTestNodes.AddRange(toAdd);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Added {Count} test nodes to test {TestId}", toAdd.Count, testId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding test nodes for test {TestId}", testId);
                throw;
            }
        }

        // ── Toggle IsActive on test nodes ────────────────────────────────────

        public async Task ToggleTestNodesActiveAsync(List<int> testNodeIds)
        {
            try
            {
                var nodes = await _context.KTestNodes
                    .Where(tn => testNodeIds.Contains(tn.Id))
                    .ToListAsync();

                foreach (var tn in nodes)
                    tn.IsActive = !tn.IsActive;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Toggled IsActive for {Count} test nodes", nodes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling test nodes active state");
                throw;
            }
        }

        // ── Delete test nodes ─────────────────────────────────────────────────

        public async Task DeleteTestNodesAsync(List<int> testNodeIds)
        {
            try
            {
                var nodes = await _context.KTestNodes
                    .Where(tn => testNodeIds.Contains(tn.Id))
                    .ToListAsync();

                if (nodes.Count > 0)
                {
                    _context.KTestNodes.RemoveRange(nodes);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted {Count} test nodes", nodes.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting test nodes");
                throw;
            }
        }
    }
}
