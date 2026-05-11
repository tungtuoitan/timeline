using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppModels.Utils;

namespace SuperAppDataRepositories.Repositories
{
    public class KQuestionRepository : IKQuestionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KQuestionRepository> _logger;
        private readonly IKStatusHistoryRepository _statusHistory;

        public KQuestionRepository(
            ApplicationDbContext context,
            ILogger<KQuestionRepository> logger,
            IKStatusHistoryRepository statusHistory)
        {
            _context       = context;
            _logger        = logger;
            _statusHistory = statusHistory;
        }

        // ── Get questions by node ─────────────────────────────────────────────

        public async Task<List<KQuestionEntity>> GetQuestionsByNodeAsync(int nodeId)
        {
            try
            {
                return await _context.KQuestions
                    .Where(q => q.NodeId == nodeId)
                    .OrderBy(q => q.SortOrder)
                    .ThenBy(q => q.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions for node {NodeId}", nodeId);
                throw;
            }
        }

        // ── Get orphan questions (node_id IS NULL) ────────────────────────────

        public async Task<List<KQuestionEntity>> GetAllQuestionsByKnowledgeAsync(int knowledgeId)
        {
            try
            {
                return await _context.KQuestions
                    .Include(q => q.Node)
                    .Where(q => q.Node == null || q.Node.KnowledgeId == knowledgeId)
                    .OrderBy(q => q.SortOrder)
                    .ThenBy(q => q.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all questions for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        public async Task<int> CountAllByKnowledgeAsync(int knowledgeId)
        {
            try
            {
                return await _context.KQuestions
                    .Where(q => q.Node == null || q.Node.KnowledgeId == knowledgeId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting questions for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        public async Task<List<KQuestionEntity>> GetOrphanQuestionsAsync()
        {
            try
            {
                return await _context.KQuestions
                    .Where(q => q.NodeId == null)
                    .OrderBy(q => q.SortOrder)
                    .ThenBy(q => q.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orphan questions");
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

        // ── Add questions ─────────────────────────────────────────────────────

        public async Task<List<int>> AddQuestionsAsync(int? nodeId, List<KNewQuestionItem> questions)
        {
            try
            {
                if (!questions.Any()) return [];

                var maxOrder = await _context.KQuestions
                    .Where(q => q.NodeId == nodeId)
                    .Select(q => (int?)q.SortOrder)
                    .MaxAsync() ?? -1;

                var rows = questions.Select((q, i) => new KQuestionEntity
                {
                    NodeId      = nodeId,
                    Name        = q.Name,
                    Description = q.Description,
                    StatusCode  = "learning",
                    SortOrder   = maxOrder + 1 + i,
                }).ToList();

                _context.KQuestions.AddRange(rows);
                await _context.SaveChangesAsync();

                await _statusHistory.AddQuestionStatusBulkAsync(
                    rows.Select(r => (r.Id, r.StatusCode)),
                    userId: null);

                _logger.LogInformation("Added {Count} questions to node {NodeId}", rows.Count, nodeId);
                return rows.Select(r => r.Id).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding questions to node {NodeId}", nodeId);
                throw;
            }
        }

        // ── Update question name/description ─────────────────────────────────

        public async Task UpdateQuestionsDataAsync(List<KUpdateQuestionItem> updates)
        {
            try
            {
                if (!updates.Any()) return;
                var ids       = updates.Select(u => u.Id).ToList();
                var questions = await _context.KQuestions
                    .Where(q => ids.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                {
                    var upd = updates.FirstOrDefault(u => u.Id == q.Id);
                    if (upd == null) continue;
                    q.Name        = upd.Name;
                    q.Description = string.IsNullOrWhiteSpace(upd.Description) ? null : upd.Description;
                    q.UpdatedAt   = VietnamDateTime.Now();
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

        // ── Soft-delete questions ─────────────────────────────────────────────

        public async Task DeleteQuestionsAsync(List<int> questionIds)
        {
            try
            {
                var questions = await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                    q.DeletedAt = VietnamDateTime.Now();

                await _context.SaveChangesAsync();
                _logger.LogInformation("Soft-deleted {Count} questions", questions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting questions");
                throw;
            }
        }

        // ── Restore questions ─────────────────────────────────────────────────

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

        // ── Reset SRS state ───────────────────────────────────────────────────

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
                    q.SrsInterval     = 0;
                    q.SrsEaseFactor   = 2.5;
                    q.SrsRepetitions  = 0;
                    q.SrsNextReviewAt = null;
                }

                // Also clear point history for these questions
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

        // ── Mark single question as draft + clear all review history ─────────

        public async Task MarkQuestionDraftAsync(int questionId)
        {
            try
            {
                var question = await _context.KQuestions.FindAsync(questionId);
                if (question == null) return;

                var wasDraftAlready       = question.StatusCode == "draft";
                question.StatusCode        = "draft";
                question.SrsInterval      = 0;
                question.SrsEaseFactor    = 2.5;
                question.SrsRepetitions   = 0;
                question.SrsNextReviewAt  = null;
                question.UpdatedAt        = VietnamDateTime.Now();

                var history = await _context.KPointHistory
                    .Where(p => p.QuestionId == questionId)
                    .ToListAsync();
                _context.KPointHistory.RemoveRange(history);

                await _context.SaveChangesAsync();

                if (!wasDraftAlready)
                    await _statusHistory.AddQuestionStatusAsync(questionId, "draft", userId: null);

                _logger.LogInformation("Marked question {QuestionId} as draft, deleted {Count} history rows", questionId, history.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking question {QuestionId} as draft", questionId);
                throw;
            }
        }

        // ── Toggle draft flag ─────────────────────────────────────────────────

        public async Task ToggleQuestionsDraftAsync(List<int> questionIds)
        {
            try
            {
                if (!questionIds.Any()) return;
                var questions = await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                    q.StatusCode = q.StatusCode == "draft" ? "learning" : "draft";

                await _context.SaveChangesAsync();

                await _statusHistory.AddQuestionStatusBulkAsync(
                    questions.Select(q => (q.Id, q.StatusCode)),
                    userId: null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling draft for questions");
                throw;
            }
        }

        // ── Move question to different node ──────────────────────────────────

        public async Task MoveQuestionAsync(int questionId, int? targetNodeId)
        {
            try
            {
                var q = await _context.KQuestions.FindAsync(questionId);
                if (q == null) return;
                q.NodeId    = targetNodeId;
                q.UpdatedAt = VietnamDateTime.Now();
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving question {QuestionId} to node {TargetNodeId}", questionId, targetNodeId);
                throw;
            }
        }

        // ── Save submission ───────────────────────────────────────────────────

        public async Task SaveSubmissionAsync(
            int? nodeId,
            int userId,
            List<(int QuestionId, string? AnswerText, int Point)> results)
        {
            try
            {
                var rows = results.Select(r => new KPointHistoryEntity
                {
                    KnowledgeId = nodeId,   // stores nodeId for score lookups
                    UserId      = userId,
                    QuestionId  = r.QuestionId,
                    AnswerText  = r.AnswerText,
                    Point       = r.Point,
                    CreatedAt   = VietnamDateTime.Now(),
                }).ToList();

                _context.KPointHistory.AddRange(rows);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved {Count} point history rows for node {NodeId}", rows.Count, nodeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving submission for node {NodeId}, user {UserId}", nodeId, userId);
                throw;
            }
        }

        // ── Save daily submission ─────────────────────────────────────────────

        public async Task SaveDailySubmissionAsync(
            int? knowledgeId,
            int userId,
            List<(int QuestionId, string? AnswerText, int Point, int? ResponseTimeMs)> results)
        {
            try
            {
                var rows = results.Select(r => new KPointHistoryEntity
                {
                    KnowledgeId    = knowledgeId,
                    UserId         = userId,
                    QuestionId     = r.QuestionId,
                    AnswerText     = r.AnswerText,
                    Point          = r.Point,
                    ResponseTimeMs = r.ResponseTimeMs,
                    CreatedAt      = VietnamDateTime.Now(),
                }).ToList();

                _context.KPointHistory.AddRange(rows);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving daily submission for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        // ── Get history for questions ─────────────────────────────────────────

        public async Task<List<KPointHistoryEntity>> GetHistoryForQuestionsAsync(int userId, List<int> questionIds)
        {
            try
            {
                if (!questionIds.Any()) return [];
                return await _context.KPointHistory
                    .Where(p => p.UserId == userId
                             && p.QuestionId != null && questionIds.Contains(p.QuestionId.Value))
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question history for user {UserId}", userId);
                return [];
            }
        }

        // ── Get all history for questions (for retention graph) ───────────────

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

        // ── Question scores (latest point per question) ───────────────────────

        public async Task<Dictionary<int, int>> GetQuestionScoresAsync(int nodeId, int userId)
        {
            try
            {
                // Get all question IDs for this node, then fetch their latest scores
                var questionIds = await _context.KQuestions
                    .Where(q => q.NodeId == nodeId)
                    .Select(q => q.Id)
                    .ToListAsync();

                if (!questionIds.Any()) return [];

                var rows = await _context.KPointHistory
                    .Where(p => p.UserId == userId && p.QuestionId.HasValue && questionIds.Contains(p.QuestionId.Value))
                    .GroupBy(p => p.QuestionId!.Value)
                    .Select(g => new { QuestionId = g.Key, Point = g.OrderByDescending(p => p.CreatedAt).First().Point })
                    .ToListAsync();

                return rows.ToDictionary(r => r.QuestionId, r => r.Point);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question scores for node {NodeId}, user {UserId}", nodeId, userId);
                throw;
            }
        }

        // ── Update SRS state ──────────────────────────────────────────────────

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
                question.UpdatedAt       = VietnamDateTime.Now();

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SRS for question {QuestionId}", questionId);
                throw;
            }
        }

        // ── Bulk-update status_code ───────────────────────────────────────────────

        public async Task UpdateQuestionsStatusAsync(List<int> questionIds, string statusCode)
        {
            try
            {
                if (!questionIds.Any()) return;
                var questions = await _context.KQuestions
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                foreach (var q in questions)
                {
                    q.StatusCode = statusCode;
                    q.UpdatedAt  = VietnamDateTime.Now();
                }

                await _context.SaveChangesAsync();

                await _statusHistory.AddQuestionStatusBulkAsync(
                    questions.Select(q => (q.Id, statusCode)),
                    userId: null);

                _logger.LogInformation(
                    "Status → {Status} for {Count} questions: [{Ids}]",
                    statusCode, questions.Count, string.Join(',', questionIds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status to {Status} for questions {Ids}",
                    statusCode, string.Join(',', questionIds));
                throw;
            }
        }

        // ── Daily queue ───────────────────────────────────────────────────────

        public async Task<List<KDailyQueueItem>> GetDailyQueueAsync(int knowledgeId, int userId)
        {
            try
            {
                var now = VietnamDateTime.Now();

                var knowledge = await _context.KKnowledges.FindAsync(knowledgeId);
                if (knowledge == null) return [];

                var questions = await _context.KQuestions
                    .Include(q => q.Node)
                    .Where(q => q.Node != null
                             && q.Node.KnowledgeId == knowledgeId
                             && q.Node.StatusCode == "learning"
                             && q.Node.DeletedAt == null
                             && q.DeletedAt == null)
                    .ToListAsync();

                if (!questions.Any()) return [];

                var reviewable = questions.Where(q => q.StatusCode == "learning").ToList();
                var item = new KDailyQueueItem
                {
                    KnowledgeId   = knowledgeId,
                    KnowledgeName = knowledge.Name,
                    DueCount      = reviewable.Count(q => q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now),
                    NewCount      = reviewable.Count(q => q.SrsNextReviewAt == null),
                    ActiveCount   = reviewable.Count,
                };

                return [item];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily queue for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        // ── Global daily queue ────────────────────────────────────────────────

        //public async Task<List<KDailyQueueItem>> GetGlobalDailyQueueAsync(int userId)
        //{
        //    try
        //    {
        //        var now = VietnamDateTime.Now();

        //        var knowledges = await _context.KKnowledges
        //            .Where(k => k.UserId == userId && k.DeletedAt == null && k.StatusCode == "active")
        //            .OrderBy(k => k.Name)
        //            .ToListAsync();

        //        if (!knowledges.Any()) return [];

        //        var knowledgeIds = knowledges.Select(k => k.Id).ToList();
        //        var questions = await _context.KQuestions
        //            .Include(q => q.Node)
        //            .Where(q => q.Node != null
        //                     && knowledgeIds.Contains(q.Node.KnowledgeId)
        //                     && q.Node.StatusCode == "learning"
        //                     && q.Node.DeletedAt == null
        //                     && q.DeletedAt == null)
        //            .ToListAsync();

        //        return knowledges
        //            .Select(k =>
        //            {
        //                var qs         = questions.Where(q => q.Node!.KnowledgeId == k.Id).ToList();
        //                var reviewable = qs.Where(q => q.StatusCode == "learning").ToList();
        //                return new KDailyQueueItem
        //                {
        //                    KnowledgeId   = k.Id,
        //                    KnowledgeName = k.Name,
        //                    DueCount      = reviewable.Count(q => q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now),
        //                    NewCount      = reviewable.Count(q => q.SrsNextReviewAt == null),
        //                    ActiveCount   = qs.Count,
        //                    DraftCount    = qs.Count(q => q.StatusCode == "draft"),
        //                };
        //            })
        //            .Where(item => item.DueCount + item.NewCount + item.DraftCount > 0)
        //            .ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting global daily queue for user {UserId}", userId);
        //        throw;
        //    }
        //}

        // ── Daily session questions ───────────────────────────────────────────

        public async Task<List<KQuestionEntity>> GetDailySessionQuestionsAsync(int nodeId, int dailyLimit, double newRatio)
        {
            try
            {
                var now      = VietnamDateTime.Now();
                var dueLimit = (int)Math.Ceiling(dailyLimit * (1 - newRatio));
                var newLimit = dailyLimit - dueLimit;

                var dueQuestions = await _context.KQuestions
                    .Where(q => q.NodeId == nodeId && q.StatusCode == "learning"
                             && q.DeletedAt == null && q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now)
                    .OrderBy(q => q.SrsNextReviewAt)
                    .Take(dueLimit)
                    .ToListAsync();

                var newQuestions = await _context.KQuestions
                    .Where(q => q.NodeId == nodeId && q.StatusCode == "learning" && q.DeletedAt == null
                             && q.SrsNextReviewAt == null)
                    .OrderBy(q => q.SortOrder)
                    .Take(newLimit)
                    .ToListAsync();

                // Fill remaining slots from the other pool if one runs short
                var remaining = dailyLimit - dueQuestions.Count - newQuestions.Count;
                if (remaining > 0 && dueQuestions.Count < dueLimit)
                {
                    var existingIds = dueQuestions.Select(q => q.Id).Concat(newQuestions.Select(q => q.Id)).ToHashSet();
                    var extra = await _context.KQuestions
                        .Where(q => q.NodeId == nodeId && q.StatusCode == "learning" && q.DeletedAt == null
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
                        .Where(q => q.NodeId == nodeId && q.StatusCode == "learning" && q.DeletedAt == null
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
                _logger.LogError(ex, "Error getting daily session questions for node {NodeId}", nodeId);
                throw;
            }
        }

        // ── Knowledge-level daily session (all nodes in a knowledge) ─────────

        public async Task<List<KQuestionEntity>> GetKnowledgeDailySessionQuestionsAsync(int knowledgeId, int dailyLimit, double newRatio)
        {
            try
            {
                var now      = VietnamDateTime.Now();
                var dueLimit = (int)Math.Ceiling(dailyLimit * (1 - newRatio));
                var newLimit = dailyLimit - dueLimit;

                var dueQuestions = await _context.KQuestions
                    .Include(q => q.Node)
                    .Where(q => q.Node != null && q.Node.KnowledgeId == knowledgeId && q.Node.DeletedAt == null
                             && q.StatusCode == "learning"
                             && q.DeletedAt == null && q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now)
                    .OrderBy(q => q.SrsNextReviewAt)
                    .Take(dueLimit)
                    .ToListAsync();

                var newQuestions = await _context.KQuestions
                    .Include(q => q.Node)
                    .Where(q => q.Node != null && q.Node.KnowledgeId == knowledgeId && q.Node.DeletedAt == null
                             && q.StatusCode == "learning" && q.DeletedAt == null && q.SrsNextReviewAt == null)
                    .OrderBy(q => q.SortOrder)
                    .Take(newLimit)
                    .ToListAsync();

                var remaining = dailyLimit - dueQuestions.Count - newQuestions.Count;
                if (remaining > 0 && dueQuestions.Count < dueLimit)
                {
                    var existingIds = dueQuestions.Select(q => q.Id).Concat(newQuestions.Select(q => q.Id)).ToHashSet();
                    var extra = await _context.KQuestions
                        .Include(q => q.Node)
                        .Where(q => q.Node != null && q.Node.KnowledgeId == knowledgeId && q.Node.DeletedAt == null
                                 && q.StatusCode == "learning" && q.DeletedAt == null && q.SrsNextReviewAt == null
                                 && !existingIds.Contains(q.Id))
                        .OrderBy(q => q.SortOrder)
                        .Take(remaining)
                        .ToListAsync();
                    newQuestions.AddRange(extra);
                }
                else if (remaining > 0 && newQuestions.Count < newLimit)
                {
                    var existingIds = dueQuestions.Select(q => q.Id).Concat(newQuestions.Select(q => q.Id)).ToHashSet();
                    var extra = await _context.KQuestions
                        .Include(q => q.Node)
                        .Where(q => q.Node != null && q.Node.KnowledgeId == knowledgeId && q.Node.DeletedAt == null
                                 && q.StatusCode == "learning"
                                 && q.DeletedAt == null && q.SrsNextReviewAt != null && q.SrsNextReviewAt <= now
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
                _logger.LogError(ex, "Error getting knowledge daily session for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        // ── Active questions for retention ────────────────────────────────────

        public async Task<List<KQuestionEntity>> GetActiveQuestionsAsync(int knowledgeId)
        {
            try
            {
                return await _context.KQuestions
                    .Include(q => q.Node)
                    .Where(q => q.Node != null && q.Node.KnowledgeId == knowledgeId
                             && q.Node.DeletedAt == null
                             && q.DeletedAt == null
                             && q.StatusCode != "draft")
                    .OrderBy(q => q.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active questions for knowledge {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        // ── Node name lookup ──────────────────────────────────────────────────

        public async Task<string?> GetNodeNameAsync(int nodeId)
        {
            try
            {
                var node = await _context.KNodes.FindAsync(nodeId);
                return node?.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node name for {NodeId}", nodeId);
                return null;
            }
        }
    }
}
