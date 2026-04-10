using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Requests;
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

        public async Task<List<KTestSummaryResponse>> GetTestSummariesAsync(int knowledgeId, int userId, int? nodeId = null)
        {
            try
            {
                var testQuery = _context.KTests
                    .Where(t => t.KnowledgeId == knowledgeId);

                if (nodeId.HasValue)
                    testQuery = testQuery.Where(t => t.NodeId == nodeId.Value);

                var tests = await testQuery
                    .OrderBy(t => t.SortOrder)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();

                if (!tests.Any()) return [];

                var testIds = tests.Select(t => t.Id).ToList();

                // Question counts from k.question
                var questionCounts = await _context.KQuestions
                    .Where(q => testIds.Contains(q.TestId) && q.DeletedAt == null)
                    .GroupBy(q => q.TestId)
                    .Select(g => new { TestId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TestId, x => x.Count);

                var activeQuestionCounts = await _context.KQuestions
                    .Where(q => testIds.Contains(q.TestId) && q.IsActive && q.DeletedAt == null)
                    .GroupBy(q => q.TestId)
                    .Select(g => new { TestId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TestId, x => x.Count);

                // Latest point history per (test, question)
                var latestRows = await _context.KPointHistory
                    .Where(p => testIds.Contains(p.TestId) && p.UserId == userId)
                    .GroupBy(p => new { p.TestId, p.QuestionId })
                    .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                    .ToListAsync();

                var allHistory = await _context.KPointHistory
                    .Where(p => testIds.Contains(p.TestId) && p.UserId == userId)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

                var scoreHistoryByTest = allHistory
                    .GroupBy(p => p.TestId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .GroupBy(p => new DateTime(
                                p.CreatedAt.Year, p.CreatedAt.Month, p.CreatedAt.Day,
                                p.CreatedAt.Hour, p.CreatedAt.Minute, p.CreatedAt.Second))
                            .OrderBy(sg => sg.Key)
                            .Select(sg =>
                            {
                                var total  = sg.Sum(x => x.Point);
                                var maxPts = sg.Count() * 5;
                                return maxPts > 0 ? (int)Math.Round((double)total / maxPts * 100) : 0;
                            })
                            .TakeLast(10)
                            .ToList()
                    );

                return tests.Select(t =>
                {
                    var rows         = latestRows.Where(r => r.TestId == t.Id).ToList();
                    var questionCount = questionCounts.TryGetValue(t.Id, out var c) ? c : 0;
                    var activeCount  = activeQuestionCounts.TryGetValue(t.Id, out var ac) ? ac : 0;
                    int? total = rows.Any() ? rows.Sum(r => r.Point) : null;
                    int? max   = rows.Any() ? questionCount * 5 : null;
                    int? pct   = total.HasValue && max > 0
                        ? (int)Math.Round((double)total.Value / max.Value * 100) : null;

                    return new KTestSummaryResponse
                    {
                        Id              = t.Id,
                        KnowledgeId     = t.KnowledgeId,
                        UserId          = t.UserId,
                        Title           = t.Title,
                        Level           = t.Level,
                        Mode            = t.Mode,
                        Status          = t.Status,
                        NodeCount       = questionCount,
                        ActiveCount     = activeCount,
                        LastTotalPoints = total,
                        LastMaxPoints   = max,
                        LastPct         = pct,
                        LastSubmittedAt = rows.Any() ? rows.Max(r => r.CreatedAt) : null,
                        CreatedAt       = t.CreatedAt,
                        SortOrder       = t.SortOrder,
                        DeletedAt       = t.DeletedAt,
                        NodeId          = t.NodeId,
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

        // ── Get test by ID (with questions) ──────────────────────────────────

        public async Task<KTestEntity?> GetTestByIdAsync(int testId, int knowledgeId)
        {
            try
            {
                return await _context.KTests
                    .Include(t => t.Questions)
                    .FirstOrDefaultAsync(t => t.Id == testId && t.KnowledgeId == knowledgeId && t.DeletedAt == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test ID {TestId} for knowledge {KnowledgeId}", testId, knowledgeId);
                throw;
            }
        }

        // ── Create test + k.question rows ────────────────────────────────────

        public async Task<KTestEntity> CreateTestAsync(KTestEntity test, List<(string Name, string? Description)> questions)
        {
            try
            {
                _context.KTests.Add(test);
                await _context.SaveChangesAsync();

                if (questions.Any())
                {
                    var rows = questions.Select((q, i) => new KQuestionEntity
                    {
                        TestId      = test.Id,
                        Name        = q.Name,
                        Description = q.Description,
                        IsActive    = true,
                        SortOrder   = i,
                    }).ToList();

                    _context.KQuestions.AddRange(rows);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created test ID {Id} with {Count} questions", test.Id, questions.Count);
                return test;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test for knowledge {KnowledgeId}", test.KnowledgeId);
                throw;
            }
        }

        // ── Get questions by IDs ──────────────────────────────────────────────

        public async Task<List<KQuestionEntity>> GetQuestionsByIdsAsync(List<int> questionIds)
        {
            try
            {
                if (!questionIds.Any()) return [];
                return await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions by IDs");
                throw;
            }
        }

        // ── Add questions to test ─────────────────────────────────────────────

        public async Task AddQuestionsAsync(int testId, List<KNewQuestionItem> questions)
        {
            try
            {
                if (!questions.Any()) return;

                var maxOrder = await _context.KQuestions
                    .Where(q => q.TestId == testId)
                    .Select(q => (int?)q.SortOrder)
                    .MaxAsync() ?? -1;

                var rows = questions.Select((q, i) => new KQuestionEntity
                {
                    TestId      = testId,
                    Name        = q.Name,
                    Description = q.Description,
                    IsActive    = true,
                    SortOrder   = maxOrder + 1 + i,
                }).ToList();

                _context.KQuestions.AddRange(rows);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added {Count} questions to test {TestId}", rows.Count, testId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding questions to test {TestId}", testId);
                throw;
            }
        }

        // ── Update question name/description ─────────────────────────────────

        public async Task UpdateQuestionsDataAsync(List<KUpdateQuestionItem> updates)
        {
            try
            {
                if (!updates.Any()) return;
                var ids = updates.Select(u => u.Id).ToList();
                var questions = await _context.KQuestions
                    .Where(q => ids.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                {
                    var upd = updates.FirstOrDefault(u => u.Id == q.Id);
                    if (upd == null) continue;
                    q.Name        = upd.Name;
                    q.Description = string.IsNullOrWhiteSpace(upd.Description) ? null : upd.Description;
                    q.UpdatedAt   = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} questions", questions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating questions data");
                throw;
            }
        }

        // ── Toggle IsActive on questions ──────────────────────────────────────

        public async Task ToggleQuestionsActiveAsync(List<int> questionIds)
        {
            try
            {
                var questions = await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                    q.IsActive = !q.IsActive;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Toggled IsActive for {Count} questions", questions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling questions active state");
                throw;
            }
        }

        // ── Soft-delete questions ─────────────────────────────────────────────

        public async Task DeleteQuestionsAsync(List<int> questionIds)
        {
            try
            {
                var questions = await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                    q.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Soft-deleted {Count} questions", questions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting questions");
                throw;
            }
        }

        // ── Restore soft-deleted questions ────────────────────────────────────

        public async Task RestoreQuestionsAsync(List<int> questionIds)
        {
            try
            {
                var questions = await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                    q.DeletedAt = null;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Restored {Count} questions", questions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring questions");
                throw;
            }
        }

        // ── Reset SRS state for questions ────────────────────────────────────

        public async Task ResetQuestionsSrsAsync(List<int> questionIds)
        {
            try
            {
                if (!questionIds.Any()) return;
                var questions = await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                {
                    q.SrsInterval = 0;
                    q.SrsEaseFactor = 2.5;
                    q.SrsRepetitions = 0;
                    q.SrsNextReviewAt = null;
                }

                // Delete point history for these questions
                var history = await _context.KPointHistory
                    .Where(p => p.QuestionId.HasValue && questionIds.Contains(p.QuestionId.Value))
                    .ToListAsync();
                _context.KPointHistory.RemoveRange(history);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Reset SRS for {Count} questions, deleted {HistCount} point_history rows", questions.Count, history.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting SRS for questions");
                throw;
            }
        }

        // ── Move questions to a different test (preserves SRS/history) ────────

        public async Task MoveQuestionsAsync(List<KMoveQuestionItem> items)
        {
            try
            {
                if (!items.Any()) return;
                var questionIds = items.Select(i => i.Id).ToList();
                var questions   = await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                var lookup = items.ToDictionary(i => i.Id, i => i.TargetTestId);
                foreach (var q in questions)
                    if (lookup.TryGetValue(q.Id, out var targetId))
                        q.TestId = targetId;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Moved {Count} questions to new tests", questions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving questions");
                throw;
            }
        }

        // ── Get history for questions ─────────────────────────────────────────

        public async Task<List<KPointHistoryEntity>> GetHistoryForQuestionsAsync(int testId, int userId, List<int> questionIds)
        {
            try
            {
                return await _context.KPointHistory
                    .Where(p => p.TestId == testId && p.UserId == userId
                                && p.QuestionId != null && questionIds.Contains(p.QuestionId.Value))
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question history for test {TestId}", testId);
                return [];
            }
        }

        // ── Save submission ───────────────────────────────────────────────────

        public async Task SaveSubmissionAsync(
            int testId,
            int userId,
            List<(int QuestionId, string? AnswerText, int Point)> results)
        {
            try
            {
                var rows = results.Select(r => new KPointHistoryEntity
                {
                    TestId     = testId,
                    UserId     = userId,
                    QuestionId = r.QuestionId,
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

        // ── Get latest result for a test ──────────────────────────────────────

        public async Task<List<KPointHistoryEntity>> GetLatestResultAsync(int testId, int userId)
        {
            try
            {
                return await _context.KPointHistory
                    .Where(p => p.TestId == testId && p.UserId == userId)
                    .GroupBy(p => p.QuestionId)
                    .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest result for test {TestId}, user {UserId}", testId, userId);
                throw;
            }
        }

        // ── Question scores (latest point per question, across all tests) ─────

        public async Task<Dictionary<int, int>> GetQuestionScoresAsync(int knowledgeId, int userId)
        {
            try
            {
                var testIds = await _context.KTests
                    .Where(t => t.KnowledgeId == knowledgeId && t.DeletedAt == null)
                    .Select(t => t.Id)
                    .ToListAsync();

                if (!testIds.Any()) return [];

                var rows = await _context.KPointHistory
                    .Where(p => testIds.Contains(p.TestId) && p.UserId == userId && p.QuestionId.HasValue)
                    .GroupBy(p => p.QuestionId!.Value)
                    .Select(g => new { QuestionId = g.Key, Point = g.OrderByDescending(p => p.CreatedAt).First().Point })
                    .ToListAsync();

                return rows.ToDictionary(r => r.QuestionId, r => r.Point);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question scores for knowledge {KnowledgeId}, user {UserId}", knowledgeId, userId);
                throw;
            }
        }

        // ── Update test title ─────────────────────────────────────────────────

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

        // ── Move test to a different node ────────────────────────────────────

        public async Task<KTestEntity?> MoveTestToNodeAsync(int testId, int knowledgeId, int? nodeId)
        {
            try
            {
                var test = await _context.KTests
                    .FirstOrDefaultAsync(t => t.Id == testId && t.KnowledgeId == knowledgeId && t.DeletedAt == null);
                if (test == null) return null;

                // Bump sort_order of existing tests in target node
                var siblings = await _context.KTests
                    .Where(t => t.KnowledgeId == knowledgeId && t.NodeId == nodeId && t.Id != testId && t.DeletedAt == null)
                    .ToListAsync();
                foreach (var s in siblings) s.SortOrder += 1;

                test.NodeId    = nodeId;
                test.SortOrder = 0;
                test.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return test;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving test {TestId} to node {NodeId}", testId, nodeId);
                throw;
            }
        }

        // ── Reorder tests ─────────────────────────────────────────────────────

        public async Task ReorderTestsAsync(int knowledgeId, List<int> orderedTestIds)
        {
            try
            {
                var tests = await _context.KTests
                    .Where(t => t.KnowledgeId == knowledgeId && orderedTestIds.Contains(t.Id) && t.DeletedAt == null)
                    .ToListAsync();

                for (int i = 0; i < orderedTestIds.Count; i++)
                {
                    var test = tests.FirstOrDefault(t => t.Id == orderedTestIds[i]);
                    if (test != null) test.SortOrder = i;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering tests for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SRS / Daily Review
        // ══════════════════════════════════════════════════════════════════════

        public async Task<List<KDailyQueueItem>> GetDailyQueueAsync(int knowledgeId, int userId)
        {
            try
            {
                var now = DateTime.UtcNow;

                var knowledge = await _context.KKnowledges.FindAsync(knowledgeId);

                var tests = await _context.KTests
                    .Where(t => t.KnowledgeId == knowledgeId
                             && t.DeletedAt == null
                             && (t.Status == "learning" || t.Status == "mastered"))
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();

                if (!tests.Any()) return [];

                var testIds = tests.Select(t => t.Id).ToList();

                var questions = await _context.KQuestions
                    .Where(q => testIds.Contains(q.TestId) && q.IsActive && q.DeletedAt == null)
                    .ToListAsync();

                return tests.Select(t =>
                {
                    var qs = questions.Where(q => q.TestId == t.Id).ToList();
                    return new KDailyQueueItem
                    {
                        TestId        = t.Id,
                        KnowledgeId   = knowledgeId,
                        KnowledgeName = knowledge?.Name ?? "",
                        Title         = t.Title,
                        Level         = t.Level,
                        Status        = t.Status,
                        DueCount      = qs.Count(q => q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now),
                        NewCount      = qs.Count(q => q.SrsNextReviewAt == null),
                        ActiveCount   = qs.Count,
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily queue for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        public async Task<List<KDailyQueueItem>> GetGlobalDailyQueueAsync(int userId)
        {
            try
            {
                var now = DateTime.UtcNow;

                var tests = await _context.KTests
                    .Where(t => t.DeletedAt == null
                             && (t.Status == "learning" || t.Status == "mastered"))
                    .Join(_context.KKnowledges.Where(k => k.UserId == userId && k.DeletedAt == null),
                          t => t.KnowledgeId, k => k.Id,
                          (t, k) => new { Test = t, KnowledgeName = k.Name })
                    .OrderBy(x => x.Test.KnowledgeId)
                    .ThenBy(x => x.Test.SortOrder)
                    .ToListAsync();

                if (!tests.Any()) return [];

                var testIds = tests.Select(x => x.Test.Id).ToList();

                var questions = await _context.KQuestions
                    .Where(q => testIds.Contains(q.TestId) && q.IsActive && q.DeletedAt == null)
                    .ToListAsync();

                return tests.Select(x =>
                {
                    var qs = questions.Where(q => q.TestId == x.Test.Id).ToList();
                    return new KDailyQueueItem
                    {
                        TestId        = x.Test.Id,
                        KnowledgeId   = x.Test.KnowledgeId,
                        KnowledgeName = x.KnowledgeName,
                        Title         = x.Test.Title,
                        Level         = x.Test.Level,
                        Status        = x.Test.Status,
                        DueCount      = qs.Count(q => q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now),
                        NewCount      = qs.Count(q => q.SrsNextReviewAt == null),
                        ActiveCount   = qs.Count,
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting global daily queue for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<KQuestionEntity>> GetDailySessionQuestionsAsync(int testId, int dailyLimit, double newRatio)
        {
            try
            {
                var now = DateTime.UtcNow;
                var dueLimit = (int)Math.Ceiling(dailyLimit * (1 - newRatio));
                var newLimit = dailyLimit - dueLimit;

                var dueQuestions = await _context.KQuestions
                    .Where(q => q.TestId == testId && q.IsActive && q.DeletedAt == null
                             && q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now)
                    .OrderBy(q => q.SrsNextReviewAt)
                    .Take(dueLimit)
                    .ToListAsync();

                var newQuestions = await _context.KQuestions
                    .Where(q => q.TestId == testId && q.IsActive && q.DeletedAt == null
                             && q.SrsNextReviewAt == null)
                    .OrderBy(q => q.SortOrder)
                    .Take(newLimit)
                    .ToListAsync();

                // If fewer due, fill remaining with new, and vice versa
                var remaining = dailyLimit - dueQuestions.Count - newQuestions.Count;
                if (remaining > 0 && dueQuestions.Count < dueLimit)
                {
                    var existingIds = dueQuestions.Select(q => q.Id).Concat(newQuestions.Select(q => q.Id)).ToHashSet();
                    var extra = await _context.KQuestions
                        .Where(q => q.TestId == testId && q.IsActive && q.DeletedAt == null
                                 && q.SrsNextReviewAt == null && !existingIds.Contains(q.Id))
                        .OrderBy(q => q.SortOrder)
                        .Take(remaining)
                        .ToListAsync();
                    newQuestions.AddRange(extra);
                }
                else if (remaining > 0 && newQuestions.Count < newLimit)
                {
                    var existingIds = dueQuestions.Select(q => q.Id).Concat(newQuestions.Select(q => q.Id)).ToHashSet();
                    var extra = await _context.KQuestions
                        .Where(q => q.TestId == testId && q.IsActive && q.DeletedAt == null
                                 && q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now
                                 && !existingIds.Contains(q.Id))
                        .OrderBy(q => q.SrsNextReviewAt)
                        .Take(remaining)
                        .ToListAsync();
                    dueQuestions.AddRange(extra);
                }

                var result = new List<KQuestionEntity>();
                result.AddRange(dueQuestions);
                result.AddRange(newQuestions);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily session questions for test {TestId}", testId);
                throw;
            }
        }

        public async Task UpdateQuestionSrsAsync(int questionId, int interval, double easeFactor, int repetitions, DateTime? nextReviewAt)
        {
            try
            {
                var question = await _context.KQuestions.FindAsync(questionId);
                if (question == null) return;

                question.SrsInterval     = interval;
                question.SrsEaseFactor   = easeFactor;
                question.SrsRepetitions  = repetitions;
                question.SrsNextReviewAt = nextReviewAt;
                question.UpdatedAt       = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SRS for question {QuestionId}", questionId);
                throw;
            }
        }

        public async Task SaveDailySubmissionAsync(
            int testId, int userId,
            List<(int QuestionId, string? AnswerText, int Point, int? ResponseTimeMs)> results)
        {
            try
            {
                var rows = results.Select(r => new KPointHistoryEntity
                {
                    TestId         = testId,
                    UserId         = userId,
                    QuestionId     = r.QuestionId,
                    AnswerText     = r.AnswerText,
                    Point          = r.Point,
                    ResponseTimeMs = r.ResponseTimeMs,
                    CreatedAt      = DateTime.UtcNow,
                }).ToList();

                _context.KPointHistory.AddRange(rows);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving daily submission for test {TestId}", testId);
                throw;
            }
        }

        public async Task UpdateTestStatusAsync(int testId, string status)
        {
            try
            {
                var test = await _context.KTests.FindAsync(testId);
                if (test == null) return;
                test.Status    = status;
                test.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test status for {TestId}", testId);
                throw;
            }
        }

        public async Task SoftDeleteTestAsync(int testId, int knowledgeId)
        {
            try
            {
                var test = await _context.KTests.FirstOrDefaultAsync(t => t.Id == testId && t.KnowledgeId == knowledgeId);
                if (test == null) return;
                test.DeletedAt = DateTime.UtcNow;
                test.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting test {TestId}", testId);
                throw;
            }
        }

        public async Task RestoreTestAsync(int testId, int knowledgeId)
        {
            try
            {
                var test = await _context.KTests.FirstOrDefaultAsync(t => t.Id == testId && t.KnowledgeId == knowledgeId);
                if (test == null) return;
                test.DeletedAt = null;
                test.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring test {TestId}", testId);
                throw;
            }
        }

        public async Task<List<KSubmissionGroup>> GetRecentSubmissionGroupsAsync(int testId, int userId, int count)
        {
            try
            {
                // Group point_history rows by session (same second = same session)
                var rows = await _context.KPointHistory
                    .Where(p => p.TestId == testId && p.UserId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var groups = rows
                    .GroupBy(p => new DateTime(
                        p.CreatedAt.Year, p.CreatedAt.Month, p.CreatedAt.Day,
                        p.CreatedAt.Hour, p.CreatedAt.Minute, p.CreatedAt.Second))
                    .OrderByDescending(g => g.Key)
                    .Take(count)
                    .Select(g =>
                    {
                        var items = g.ToList();
                        var avgPoint = items.Average(p => p.Point);

                        // speed_ratio = responseTimeMs / readingTimeMs
                        // readingTimeMs = wordCount(answer) / (200/60) * 1000
                        var speedRatios = new List<double>();
                        foreach (var p in items)
                        {
                            if (p.ResponseTimeMs == null || p.ResponseTimeMs <= 0) continue;
                            // We need the expected answer length — look up the question
                            var question = _context.KQuestions.Local
                                .FirstOrDefault(q => q.Id == p.QuestionId)
                                ?? _context.KQuestions.Find(p.QuestionId);
                            if (question?.Description == null) continue;
                            var wordCount = question.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                            if (wordCount == 0) continue;
                            var readingTimeMs = wordCount / (200.0 / 60.0) * 1000.0;
                            speedRatios.Add(p.ResponseTimeMs.Value / readingTimeMs);
                        }

                        return new KSubmissionGroup
                        {
                            SessionTime   = g.Key,
                            AvgPoint      = avgPoint,
                            AvgSpeedRatio = speedRatios.Count > 0 ? speedRatios.Average() : double.MaxValue,
                        };
                    })
                    .ToList();

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent submission groups for test {TestId}", testId);
                throw;
            }
        }

        public async Task<List<(KTestEntity Test, List<KQuestionEntity> Questions)>> GetTestsWithQuestionsAsync(int knowledgeId)
        {
            try
            {
                var tests = await _context.KTests
                    .Where(t => t.KnowledgeId == knowledgeId && t.DeletedAt == null)
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();

                if (!tests.Any()) return [];

                var testIds = tests.Select(t => t.Id).ToList();
                var questions = await _context.KQuestions
                    .Where(q => testIds.Contains(q.TestId) && q.IsActive && q.DeletedAt == null)
                    .ToListAsync();

                return tests.Select(t => (t, questions.Where(q => q.TestId == t.Id).ToList())).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tests with questions for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        public async Task<List<KPointHistoryEntity>> GetAllHistoryForQuestionsAsync(List<int> questionIds)
        {
            try
            {
                return await _context.KPointHistory
                    .Where(p => p.QuestionId != null && questionIds.Contains(p.QuestionId.Value))
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all history for questions");
                throw;
            }
        }
    }
}
