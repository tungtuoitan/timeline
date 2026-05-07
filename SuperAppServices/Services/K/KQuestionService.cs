using Microsoft.Extensions.Logging;
using SuperAppModels.Utils;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;
using SuperAppModels.Models;

namespace SuperAppServices.Services.K
{
    public class KQuestionService : IKQuestionService
    {
        private readonly IKQuestionRepository _repo;
        private readonly IKGradingService _grading;
        private readonly ILogger<KQuestionService> _logger;

        public KQuestionService(IKQuestionRepository repo, IKGradingService grading, ILogger<KQuestionService> logger)
        {
            _repo    = repo    ?? throw new ArgumentNullException(nameof(repo));
            _grading = grading ?? throw new ArgumentNullException(nameof(grading));
            _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
        }

        // ══════════════════════════════════════════════════════════════════════
        // CRUD
        // ══════════════════════════════════════════════════════════════════════

        public async Task<ResultOptions> GetNodeQuestionsAsync(int nodeId, int userId)
        {
            try
            {
                var questions = await _repo.GetQuestionsByNodeAsync(nodeId);
                var questionIds = questions.Select(q => q.Id).ToList();

                var allHistory = await _repo.GetHistoryForQuestionsAsync(userId, questionIds);
                var historyByQuestion = allHistory
                    .GroupBy(h => h.QuestionId)
                    .ToDictionary(
                        g => g.Key!.Value,
                        g => g.OrderBy(h => h.CreatedAt)
                              .TakeLast(10)
                              .Select(h => h.Point)
                              .ToList());

                var response = new KQuestionsListResponse
                {
                    KnowledgeId = nodeId,
                    Questions   = questions
                        .Select(q => new KQuestionResponse
                        {
                            Id              = q.Id,
                            NodeId          = q.NodeId,
                            NodeName        = q.Node?.Name ?? string.Empty,
                            Question        = q.Name,
                            Answer          = q.Description,
                            StatusCode      = q.StatusCode,
                            SortOrder       = q.SortOrder,
                            DeletedAt       = q.DeletedAt,
                            ScoreHistory    = historyByQuestion.TryGetValue(q.Id, out var hist) ? hist : [],
                            SrsNextReviewAt = q.SrsNextReviewAt,
                            Retention       = SpacedRepetitionEngine.CalculateRetention(q.SrsInterval, q.SrsNextReviewAt),
                        }).ToList(),
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetNodeQuestions failed for node {NodeId}", nodeId);
                return Fail(500, "Failed to load questions");
            }
        }

        public async Task<ResultOptions> GetKnowledgeQuestionsAsync(int knowledgeId, int userId)
        {
            try
            {
                var questions   = await _repo.GetAllQuestionsByKnowledgeAsync(knowledgeId);
                var questionIds = questions.Select(q => q.Id).ToList();

                var allHistory = await _repo.GetHistoryForQuestionsAsync(userId, questionIds);
                var historyByQuestion = allHistory
                    .GroupBy(h => h.QuestionId)
                    .ToDictionary(
                        g => g.Key!.Value,
                        g => g.OrderBy(h => h.CreatedAt)
                              .TakeLast(10)
                              .Select(h => h.Point)
                              .ToList());

                var response = new KQuestionsListResponse
                {
                    KnowledgeId = knowledgeId,
                    Questions   = questions
                        .Select(q => new KQuestionResponse
                        {
                            Id              = q.Id,
                            NodeId          = q.NodeId,
                            NodeName        = q.Node?.Name ?? string.Empty,
                            Question        = q.Name,
                            Answer          = q.Description,
                            StatusCode      = q.StatusCode,
                            SortOrder       = q.SortOrder,
                            DeletedAt       = q.DeletedAt,
                            ScoreHistory    = historyByQuestion.TryGetValue(q.Id, out var hist) ? hist : [],
                            SrsNextReviewAt = q.SrsNextReviewAt,
                            Retention       = SpacedRepetitionEngine.CalculateRetention(q.SrsInterval, q.SrsNextReviewAt),
                        }).ToList(),
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetKnowledgeQuestions failed for knowledge {KnowledgeId}", knowledgeId);
                return Fail(500, "Failed to load questions");
            }
        }

        public async Task<ResultOptions> GetOrphanQuestionsAsync(int userId)
        {
            try
            {
                var questions   = await _repo.GetOrphanQuestionsAsync();
                var questionIds = questions.Select(q => q.Id).ToList();

                var allHistory = await _repo.GetHistoryForQuestionsAsync(userId, questionIds);
                var historyByQuestion = allHistory
                    .GroupBy(h => h.QuestionId)
                    .ToDictionary(
                        g => g.Key!.Value,
                        g => g.OrderBy(h => h.CreatedAt)
                              .TakeLast(10)
                              .Select(h => h.Point)
                              .ToList());

                var response = new KQuestionsListResponse
                {
                    KnowledgeId = null,
                    Questions   = questions
                        .Select(q => new KQuestionResponse
                        {
                            Id              = q.Id,
                            Question        = q.Name,
                            Answer          = q.Description,
                            StatusCode      = q.StatusCode,
                            SortOrder       = q.SortOrder,
                            DeletedAt       = q.DeletedAt,
                            ScoreHistory    = historyByQuestion.TryGetValue(q.Id, out var hist) ? hist : [],
                            SrsNextReviewAt = q.SrsNextReviewAt,
                            Retention       = SpacedRepetitionEngine.CalculateRetention(q.SrsInterval, q.SrsNextReviewAt),
                        }).ToList(),
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrphanQuestions failed");
                return Fail(500, "Failed to load orphan questions");
            }
        }

        public async Task<ResultOptions> UpdateOrphanQuestionsAsync(int userId, KUpdateQuestionsRequest request)
        {
            try
            {
                if (request.UpdateQuestions.Count > 0)
                    await _repo.UpdateQuestionsDataAsync(request.UpdateQuestions);

                if (request.AddQuestions.Count > 0)
                    await _repo.AddQuestionsAsync(null, request.AddQuestions);   // null nodeId = orphan

                if (request.DeleteQuestionIds.Count > 0)
                    await _repo.DeleteQuestionsAsync(request.DeleteQuestionIds);

                if (request.RestoreQuestionIds.Count > 0)
                    await _repo.RestoreQuestionsAsync(request.RestoreQuestionIds);

                if (request.ResetSrsQuestionIds.Count > 0)
                    await _repo.ResetQuestionsSrsAsync(request.ResetSrsQuestionIds);

                if (request.ToggleDraftQuestionIds.Count > 0)
                    await _repo.ToggleQuestionsDraftAsync(request.ToggleDraftQuestionIds);

                return Ok(new { knowledgeId = (int?)null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateOrphanQuestions failed");
                return Fail(500, "Failed to update orphan questions");
            }
        }

        public async Task<ResultOptions> MoveQuestionAsync(int questionId, int? targetNodeId, int userId)
        {
            try
            {
                await _repo.MoveQuestionAsync(questionId, targetNodeId);
                return Ok(new { questionId, targetNodeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MoveQuestion failed for question {QuestionId}", questionId);
                return Fail(500, "Failed to move question");
            }
        }

        public async Task<ResultOptions> MarkQuestionDraftAsync(int questionId)
        {
            try
            {
                await _repo.MarkQuestionDraftAsync(questionId);
                return Ok(new { questionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkQuestionDraft failed for question {QuestionId}", questionId);
                return Fail(500, "Failed to mark question as draft");
            }
        }

        public async Task<ResultOptions> UpdateQuestionsAsync(int nodeId, int userId, KUpdateQuestionsRequest request)
        {
            try
            {
                if (request.UpdateQuestions.Count > 0)
                    await _repo.UpdateQuestionsDataAsync(request.UpdateQuestions);

                if (request.AddQuestions.Count > 0)
                    await _repo.AddQuestionsAsync(nodeId, request.AddQuestions);

                if (request.DeleteQuestionIds.Count > 0)
                    await _repo.DeleteQuestionsAsync(request.DeleteQuestionIds);

                if (request.RestoreQuestionIds.Count > 0)
                    await _repo.RestoreQuestionsAsync(request.RestoreQuestionIds);

                if (request.ResetSrsQuestionIds.Count > 0)
                    await _repo.ResetQuestionsSrsAsync(request.ResetSrsQuestionIds);

                if (request.ToggleDraftQuestionIds.Count > 0)
                    await _repo.ToggleQuestionsDraftAsync(request.ToggleDraftQuestionIds);

                return Ok(new { nodeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateQuestions failed for node {NodeId}", nodeId);
                return Fail(500, "Failed to update questions");
            }
        }

        public async Task<ResultOptions> SubmitAnswersAsync(int nodeId, int userId, KSubmitAnswersRequest request)
        {
            try
            {
                if (request.Answers == null || !request.Answers.Any())
                    return Fail(400, "Answers are required");

                var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
                var questions   = await _repo.GetQuestionsByIdsAsync(questionIds);
                var questionMap = questions.ToDictionary(q => q.Id);

                var nodeName    = await _repo.GetNodeNameAsync(nodeId) ?? "";
                var submissions = request.Answers.Select(a => (a.QuestionId, a.AnswerText)).ToList();
                var grading     = await _grading.GradeSubmissionAsync(nodeName, submissions, questions);

                var results = grading.Answers.Select(g => (
                    g.QuestionId,
                    request.Answers.FirstOrDefault(a => a.QuestionId == g.QuestionId)?.AnswerText,
                    g.Point
                )).ToList();

                await _repo.SaveSubmissionAsync(nodeId, userId, results);

                // Update SRS for each answered question
                foreach (var g in grading.Answers)
                {
                    if (!questionMap.TryGetValue(g.QuestionId, out var q)) continue;
                    var current = new SpacedRepetitionEngine.SrsState(
                        q.SrsInterval, q.SrsEaseFactor, q.SrsRepetitions, q.SrsNextReviewAt);
                    var next = SpacedRepetitionEngine.CalculateNext(g.Point, current);
                    await _repo.UpdateQuestionSrsAsync(g.QuestionId, next.Interval, next.EaseFactor, next.Repetitions, next.NextReviewAt);
                }

                var pct = grading.MaxPoints > 0
                    ? (int)Math.Round((double)grading.TotalPoints / grading.MaxPoints * 100) : 0;

                return Ok(new KSubmitAnswersResultResponse
                {
                    TotalPoints = grading.TotalPoints,
                    MaxPoints   = grading.MaxPoints,
                    Pct         = pct,
                    Grades      = grading.Answers.Select(g =>
                    {
                        questionMap.TryGetValue(g.QuestionId, out var q);
                        return new KQuestionGradeResponse
                        {
                            QuestionId     = g.QuestionId,
                            Question       = q?.Name ?? "",
                            AnswerText     = request.Answers.FirstOrDefault(a => a.QuestionId == g.QuestionId)?.AnswerText,
                            ExpectedAnswer = q?.Description,
                            Point          = g.Point,
                            Comment        = g.Comment,
                        };
                    }).ToList(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubmitAnswers failed for node {NodeId}", nodeId);
                return Fail(500, "Failed to submit answers");
            }
        }

        public async Task<Dictionary<int, int>> GetQuestionScoresAsync(int nodeId, int userId)
        {
            try   { return await _repo.GetQuestionScoresAsync(nodeId, userId); }
            catch (Exception ex) { _logger.LogError(ex, "GetQuestionScores failed"); return []; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SRS / Daily Review
        // ══════════════════════════════════════════════════════════════════════

        public async Task<ResultOptions> GetDailyQueueAsync(int nodeId, int userId)
        {
            try
            {
                var items    = await _repo.GetDailyQueueAsync(nodeId, userId);
                var response = items.Select(MapQueueItem).ToList();
                return Ok(response);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDailyQueue failed"); return Fail(500, "Failed"); }
        }

        public async Task<ResultOptions> GetGlobalDailyQueueAsync(int userId)
        {
            try
            {
                var items    = await _repo.GetGlobalDailyQueueAsync(userId);
                var response = items.Select(MapQueueItem).ToList();
                return Ok(response);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetGlobalDailyQueue failed"); return Fail(500, "Failed"); }
        }

        private static KDailyQueueItemResponse MapQueueItem(KDailyQueueItem i) => new()
        {
            KnowledgeId   = i.KnowledgeId,
            KnowledgeName = i.KnowledgeName,
            DueCount      = i.DueCount,
            NewCount      = i.NewCount,
            ActiveCount   = i.ActiveCount,
        };

        public async Task<ResultOptions> GetDailySessionAsync(int nodeId, int userId, int dailyLimit)
        {
            try
            {
                var questions = await _repo.GetDailySessionQuestionsAsync(nodeId, dailyLimit, 0.4);
                var now = VietnamDateTime.Now();
                var response  = questions.Select(q =>
                {
                    var state = new SpacedRepetitionEngine.SrsState(
                        q.SrsInterval, q.SrsEaseFactor, q.SrsRepetitions, q.SrsNextReviewAt);
                    var previews = new Dictionary<int, long>();
                    for (var score = 1; score <= 5; score++)
                    {
                        var next = SpacedRepetitionEngine.CalculateNext(score, state);
                        previews[score] = (long)(next.NextReviewAt!.Value - now).TotalSeconds;
                    }
                    return new KDailySessionQuestionResponse
                    {
                        Id                     = q.Id,
                        Question               = q.Name,
                        Answer                 = q.Description,
                        PreviewIntervalSeconds = previews,
                    };
                }).ToList();
                return Ok(response);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDailySession failed for node {NodeId}", nodeId); return Fail(500, "Failed"); }
        }

        public async Task<ResultOptions> GetKnowledgeDailySessionAsync(int nodeId, int userId, int dailyLimit)
        {
            try
            {
                var questions = await _repo.GetKnowledgeDailySessionQuestionsAsync(nodeId, dailyLimit, 0.4);
                var now = VietnamDateTime.Now();
                var response = questions.Select(q =>
                {
                    var state = new SpacedRepetitionEngine.SrsState(
                        q.SrsInterval, q.SrsEaseFactor, q.SrsRepetitions, q.SrsNextReviewAt);
                    var previews = new Dictionary<int, long>();
                    for (var score = 1; score <= 5; score++)
                    {
                        var next = SpacedRepetitionEngine.CalculateNext(score, state);
                        previews[score] = (long)(next.NextReviewAt!.Value - now).TotalSeconds;
                    }
                    return new KDailySessionQuestionResponse
                    {
                        Id                     = q.Id,
                        Question               = q.Name,
                        Answer                 = q.Description,
                        PreviewIntervalSeconds = previews,
                    };
                }).ToList();
                return Ok(response);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetKnowledgeDailySession failed for node {NodeId}", nodeId); return Fail(500, "Failed"); }
        }

        public async Task<ResultOptions> SubmitDailyAnswersAsync(int nodeId, int userId, KDailySubmitRequest request)
        {
            try
            {
                if (request.Answers == null || !request.Answers.Any())
                    return Fail(400, "Answers are required");

                var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
                var questions   = await _repo.GetQuestionsByIdsAsync(questionIds);
                var questionMap = questions.ToDictionary(q => q.Id);

                // Use self-scores when provided; otherwise fall back to AI grading
                List<(int QuestionId, string? AnswerText, int Point, int? ResponseTimeMs)> results;
                bool isSelfGraded = request.Answers.All(a => a.SelfScore.HasValue);

                if (isSelfGraded)
                {
                    results = request.Answers
                        .Select(a => (a.QuestionId, a.AnswerText, a.SelfScore!.Value, a.ResponseTimeMs))
                        .ToList();
                }
                else
                {
                    var nodeName    = await _repo.GetNodeNameAsync(nodeId) ?? "";
                    var submissions = request.Answers.Select(a => (a.QuestionId, a.AnswerText)).ToList();
                    var grading     = await _grading.GradeSubmissionAsync(nodeName, submissions, questions);
                    results = grading.Answers.Select(g =>
                    {
                        var answer = request.Answers.FirstOrDefault(a => a.QuestionId == g.QuestionId);
                        return (g.QuestionId, answer?.AnswerText, g.Point, answer?.ResponseTimeMs);
                    }).ToList();
                }

                await _repo.SaveDailySubmissionAsync(nodeId, userId, results);

                // Update SRS for each question
                foreach (var (questionId, _, point, _) in results)
                {
                    if (!questionMap.TryGetValue(questionId, out var q)) continue;
                    var current = new SpacedRepetitionEngine.SrsState(
                        q.SrsInterval, q.SrsEaseFactor, q.SrsRepetitions, q.SrsNextReviewAt);
                    var next = SpacedRepetitionEngine.CalculateNext(point, current);
                    await _repo.UpdateQuestionSrsAsync(questionId, next.Interval, next.EaseFactor, next.Repetitions, next.NextReviewAt);
                }

                int totalPoints = results.Sum(r => r.Point);
                int maxPoints   = results.Count * 5;
                int pct         = maxPoints > 0 ? (int)Math.Round((double)totalPoints / maxPoints * 100) : 0;

                return Ok(new KSubmitAnswersResultResponse
                {
                    TotalPoints = totalPoints,
                    MaxPoints   = maxPoints,
                    Pct         = pct,
                    Grades      = results.Select(r =>
                    {
                        questionMap.TryGetValue(r.QuestionId, out var q);
                        return new KQuestionGradeResponse
                        {
                            QuestionId     = r.QuestionId,
                            Question       = q?.Name ?? "",
                            AnswerText     = r.AnswerText,
                            ExpectedAnswer = q?.Description,
                            Point          = r.Point,
                            Comment        = null,
                        };
                    }).ToList(),
                });
            }
            catch (Exception ex) { _logger.LogError(ex, "SubmitDailyAnswers failed for node {NodeId}", nodeId); return Fail(500, "Failed"); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Retention
        // ══════════════════════════════════════════════════════════════════════

        public async Task<KRetentionSummaryResponse> GetRetentionSummaryAsync(int nodeId)
        {
            try
            {
                var questions = await _repo.GetActiveQuestionsAsync(nodeId);
                if (!questions.Any()) return new KRetentionSummaryResponse();

                var retentions = questions
                    .Select(q => SpacedRepetitionEngine.CalculateRetention(q.SrsInterval, q.SrsNextReviewAt))
                    .ToList();

                return new KRetentionSummaryResponse
                {
                    Average        = retentions.Count > 0 ? Math.Round(retentions.Average(), 1) : 0,
                    TotalQuestions = retentions.Count,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRetentionSummary failed for node {NodeId}", nodeId);
                return new KRetentionSummaryResponse();
            }
        }

        public async Task<KRetentionGraphResponse> GetRetentionGraphAsync(int nodeId, int days)
        {
            try
            {
                var questions = await _repo.GetActiveQuestionsAsync(nodeId);
                if (!questions.Any()) return new KRetentionGraphResponse();

                var questionIds = questions.Select(q => q.Id).ToList();
                var allHistory  = await _repo.GetAllHistoryForQuestionsAsync(questionIds);

                var historyByQuestion = allHistory
                    .Where(h => h.QuestionId.HasValue)
                    .GroupBy(h => h.QuestionId!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderBy(h => h.CreatedAt).ToList());

                // Replay SM-2 per question → build list of (reviewDate, interval) segments
                var segmentsByQuestion = new Dictionary<int, List<(DateTime ReviewDate, int Interval)>>();
                foreach (var q in questions)
                {
                    var segments = new List<(DateTime, int)>();
                    var srs      = new SpacedRepetitionEngine.SrsState(0, 2.5, 0, null);

                    if (historyByQuestion.TryGetValue(q.Id, out var history))
                    {
                        foreach (var h in history)
                        {
                            srs = SpacedRepetitionEngine.CalculateNext(h.Point, srs);
                            segments.Add((h.CreatedAt, srs.Interval));
                        }
                    }

                    segmentsByQuestion[q.Id] = segments;
                }

                var today    = VietnamDateTime.Now().Date;
                var daysList = new List<KRetentionGraphDay>();

                for (var d = days - 1; d >= 0; d--)
                {
                    var date = d == 0 ? VietnamDateTime.Now() : today.AddDays(-d).AddHours(23).AddMinutes(59);

                    var retentions = questions.Select(q =>
                    {
                        var segments = segmentsByQuestion[q.Id];
                        if (segments.Count == 0) return 0.0;

                        (DateTime ReviewDate, int Interval)? active = null;
                        for (var i = segments.Count - 1; i >= 0; i--)
                        {
                            if (segments[i].ReviewDate <= date)
                            {
                                active = segments[i];
                                break;
                            }
                        }

                        if (active == null) return 0.0;

                        var interval   = active.Value.Interval > 0 ? active.Value.Interval : 1;
                        var daysSince  = (date - active.Value.ReviewDate).TotalDays;
                        if (daysSince < 0) daysSince = 0;
                        return Math.Round(Math.Pow(0.9, daysSince / interval) * 100, 1);
                    }).ToList();

                    var avg = retentions.Count > 0 ? Math.Round(retentions.Average(), 1) : 0;

                    daysList.Add(new KRetentionGraphDay
                    {
                        Date       = today.AddDays(-d).ToString("yyyy-MM-dd"),
                        Average    = avg,
                        Retentions = retentions,
                    });
                }

                return new KRetentionGraphResponse
                {
                    Questions = questions.Select(q => new KRetentionGraphQuestion { Id = q.Id, Name = q.Name }).ToList(),
                    Days      = daysList,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRetentionGraph failed for node {NodeId}", nodeId);
                return new KRetentionGraphResponse();
            }
        }

        private static ResultOptions Ok(object data)                  => new() { Success = true,  Object = data, Status = 200 };
        private static ResultOptions Fail(int status, string message) => new() { Success = false, Message = message, Status = status };
    }
}
