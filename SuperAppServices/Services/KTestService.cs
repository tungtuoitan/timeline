using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    public class KTestService : IKTestService
    {
        private readonly IKTestRepository _repo;
        private readonly IKGradingService _grading;
        private readonly ILogger<KTestService> _logger;

        public KTestService(IKTestRepository repo, IKGradingService grading, ILogger<KTestService> logger)
        {
            _repo    = repo    ?? throw new ArgumentNullException(nameof(repo));
            _grading = grading ?? throw new ArgumentNullException(nameof(grading));
            _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<KTestSummaryResponse>> GetTestsAsync(int knowledgeId, int userId, int? nodeId = null)
        {
            try   { return await _repo.GetTestSummariesAsync(knowledgeId, userId, nodeId); }
            catch (Exception ex) { _logger.LogError(ex, "GetTests failed"); return []; }
        }

        public async Task<ResultOptions> GetTestDetailAsync(int testId, int knowledgeId, int userId)
        {
            try
            {
                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                var questions   = test.Questions.ToList();
                var questionIds = questions.Select(q => q.Id).ToList();

                var allHistory = await _repo.GetHistoryForQuestionsAsync(test.Id, userId, questionIds);
                var historyByQuestion = allHistory
                    .GroupBy(h => h.QuestionId)
                    .ToDictionary(
                        g => g.Key!.Value,
                        g => g.OrderBy(h => h.CreatedAt)
                              .TakeLast(10)
                              .Select(h => h.Point)
                              .ToList());

                return Ok(new KTestDetailResponse
                {
                    Id          = test.Id,
                    KnowledgeId = test.KnowledgeId,
                    Title       = test.Title,
                    Level       = test.Level,
                    Mode        = test.Mode,
                    Questions   = questions
                        .OrderBy(q => q.SortOrder)
                        .Select(q => new KTestQuestionResponse
                        {
                            Id              = q.Id,
                            Question        = q.Name,
                            Answer          = q.Description,
                            IsActive        = q.IsActive,
                            SortOrder       = q.SortOrder,
                            DeletedAt       = q.DeletedAt,
                            ScoreHistory    = historyByQuestion.TryGetValue(q.Id, out var hist) ? hist : [],
                            SrsNextReviewAt = q.SrsNextReviewAt,
                            Retention       = SpacedRepetitionEngine.CalculateRetention(q.SrsInterval, q.SrsNextReviewAt),
                        }).ToList(),
                    CreatedAt   = test.CreatedAt,
                });
            }
            catch (Exception ex) { _logger.LogError(ex, "GetTestDetail failed for {TestId}", testId); return Fail(500, "Failed"); }
        }

        public async Task<ResultOptions> CreateEmptyTestAsync(int knowledgeId, int userId, KCreateEmptyTestRequest request)
        {
            try
            {
                var test = new KTestEntity
                {
                    KnowledgeId = knowledgeId,
                    UserId      = userId,
                    NodeId      = request.NodeId,
                    Title       = request.Title,
                    Mode        = "standard",
                };

                var created = await _repo.CreateTestAsync(test, []);

                return Ok(new KTestDetailResponse
                {
                    Id          = created.Id,
                    KnowledgeId = created.KnowledgeId,
                    Title       = created.Title,
                    Level       = created.Level,
                    Mode        = created.Mode,
                    Questions   = [],
                    CreatedAt   = created.CreatedAt,
                });
            }
            catch (Exception ex) { _logger.LogError(ex, "CreateEmptyTest failed"); return Fail(500, "Failed to create test"); }
        }

        public async Task<ResultOptions> SubmitAnswersAsync(int testId, int knowledgeId, int userId, KSubmitAnswersRequest request)
        {
            try
            {
                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                if (request.Answers == null || !request.Answers.Any())
                    return Fail(400, "Answers are required");

                var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
                var questions   = await _repo.GetQuestionsByIdsAsync(questionIds);
                var questionMap = questions.ToDictionary(q => q.Id);

                var submissions = request.Answers.Select(a => (a.QuestionId, a.AnswerText)).ToList();
                var grading     = await _grading.GradeSubmissionAsync(test.Title, submissions, questions);

                var results = grading.Answers.Select(g => (
                    g.QuestionId,
                    request.Answers.FirstOrDefault(a => a.QuestionId == g.QuestionId)?.AnswerText,
                    g.Point
                )).ToList();

                await _repo.SaveSubmissionAsync(testId, userId, results);

                // Update SRS for each question (same as daily review)
                foreach (var g in grading.Answers)
                {
                    if (!questionMap.TryGetValue(g.QuestionId, out var q)) continue;
                    var current = new SpacedRepetitionEngine.SrsState(
                        q.SrsInterval, q.SrsEaseFactor, q.SrsRepetitions, q.SrsNextReviewAt);
                    var next = SpacedRepetitionEngine.CalculateNext(g.Point, current);
                    await _repo.UpdateQuestionSrsAsync(g.QuestionId, next.Interval, next.EaseFactor, next.Repetitions, next.NextReviewAt);
                }

                // Check mastered/regression
                await CheckAndUpdateTestStatusAsync(testId, userId, test.Status);

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
            catch (Exception ex) { _logger.LogError(ex, "SubmitAnswers failed for test {TestId}", testId); return Fail(500, "Failed to submit answers"); }
        }

        public async Task<Dictionary<int, int>> GetQuestionScoresAsync(int knowledgeId, int userId)
        {
            try   { return await _repo.GetQuestionScoresAsync(knowledgeId, userId); }
            catch (Exception ex) { _logger.LogError(ex, "GetQuestionScores failed"); return []; }
        }

        public async Task<ResultOptions> UpdateTestAsync(int testId, int knowledgeId, int userId, KUpdateTestRequest request)
        {
            try
            {
                // Move to different node if nodeId provided
                if (request.NodeId.HasValue)
                {
                    var nodeId = request.NodeId.Value == -1 ? (int?)null : request.NodeId.Value;
                    var moved = await _repo.MoveTestToNodeAsync(testId, knowledgeId, nodeId);
                    if (moved == null) return Fail(404, "Test not found");
                }

                // Update title if provided
                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    var updated = await _repo.UpdateTestTitleAsync(testId, knowledgeId, request.Title);
                    if (updated == null) return Fail(404, "Test not found");
                }

                return Ok(new { testId });
            }
            catch (Exception ex) { _logger.LogError(ex, "UpdateTest failed for {TestId}", testId); return Fail(500, "Failed to update test"); }
        }

        public async Task<ResultOptions> UpdateQuestionsAsync(int testId, int knowledgeId, int userId, KUpdateQuestionsRequest request)
        {
            try
            {
                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                if (request.UpdateQuestions.Count > 0)
                    await _repo.UpdateQuestionsDataAsync(request.UpdateQuestions);

                if (request.AddQuestions.Count > 0)
                    await _repo.AddQuestionsAsync(testId, request.AddQuestions);

                if (request.ToggleQuestionIds.Count > 0)
                    await _repo.ToggleQuestionsActiveAsync(request.ToggleQuestionIds);

                if (request.DeleteQuestionIds.Count > 0)
                    await _repo.DeleteQuestionsAsync(request.DeleteQuestionIds);

                if (request.RestoreQuestionIds.Count > 0)
                    await _repo.RestoreQuestionsAsync(request.RestoreQuestionIds);

                if (request.ResetSrsQuestionIds.Count > 0)
                    await _repo.ResetQuestionsSrsAsync(request.ResetSrsQuestionIds);

                return Ok(new { testId });
            }
            catch (Exception ex) { _logger.LogError(ex, "UpdateQuestions failed for {TestId}", testId); return Fail(500, "Failed to update questions"); }
        }

        public async Task<ResultOptions> ReorderTestsAsync(int knowledgeId, int userId, List<int> orderedTestIds)
        {
            try
            {
                await _repo.ReorderTestsAsync(knowledgeId, orderedTestIds);
                return Ok(new { orderedTestIds });
            }
            catch (Exception ex) { _logger.LogError(ex, "ReorderTests failed"); return Fail(500, "Failed to reorder tests"); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SRS / Daily Review
        // ══════════════════════════════════════════════════════════════════════

        public async Task<ResultOptions> GetDailyQueueAsync(int knowledgeId, int userId)
        {
            try
            {
                var items = await _repo.GetDailyQueueAsync(knowledgeId, userId);
                var response = items.Select(MapQueueItem).ToList();
                return Ok(response);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDailyQueue failed"); return Fail(500, "Failed"); }
        }

        public async Task<ResultOptions> GetGlobalDailyQueueAsync(int userId)
        {
            try
            {
                var items = await _repo.GetGlobalDailyQueueAsync(userId);
                var response = items.Select(MapQueueItem).ToList();
                return Ok(response);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetGlobalDailyQueue failed"); return Fail(500, "Failed"); }
        }

        private static KDailyQueueItemResponse MapQueueItem(KDailyQueueItem i) => new()
        {
            TestId        = i.TestId,
            KnowledgeId   = i.KnowledgeId,
            KnowledgeName = i.KnowledgeName,
            Title         = i.Title,
            Level         = i.Level,
            Status        = i.Status,
            DueCount      = i.DueCount,
            NewCount      = i.NewCount,
            ActiveCount   = i.ActiveCount,
        };

        public async Task<ResultOptions> GetDailySessionAsync(int testId, int knowledgeId, int userId, int dailyLimit)
        {
            try
            {
                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                var questions = await _repo.GetDailySessionQuestionsAsync(testId, dailyLimit, 0.4);
                var response = questions.Select(q => new KDailySessionQuestionResponse
                {
                    Id       = q.Id,
                    Question = q.Name,
                    Answer   = q.Description,
                }).ToList();
                return Ok(response);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDailySession failed for test {TestId}", testId); return Fail(500, "Failed"); }
        }

        public async Task<ResultOptions> SubmitDailyAnswersAsync(int testId, int knowledgeId, int userId, KDailySubmitRequest request)
        {
            try
            {
                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                if (request.Answers == null || !request.Answers.Any())
                    return Fail(400, "Answers are required");

                var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
                var questions   = await _repo.GetQuestionsByIdsAsync(questionIds);
                var questionMap = questions.ToDictionary(q => q.Id);

                // AI grading
                var submissions = request.Answers.Select(a => (a.QuestionId, a.AnswerText)).ToList();
                var grading     = await _grading.GradeSubmissionAsync(test.Title, submissions, questions);

                // Save submission with response times
                var results = grading.Answers.Select(g =>
                {
                    var answer = request.Answers.FirstOrDefault(a => a.QuestionId == g.QuestionId);
                    return (g.QuestionId, answer?.AnswerText, g.Point, answer?.ResponseTimeMs);
                }).ToList();
                await _repo.SaveDailySubmissionAsync(testId, userId, results);

                // Update SRS for each question
                foreach (var g in grading.Answers)
                {
                    if (!questionMap.TryGetValue(g.QuestionId, out var q)) continue;
                    var current = new SpacedRepetitionEngine.SrsState(
                        q.SrsInterval, q.SrsEaseFactor, q.SrsRepetitions, q.SrsNextReviewAt);
                    var next = SpacedRepetitionEngine.CalculateNext(g.Point, current);
                    await _repo.UpdateQuestionSrsAsync(g.QuestionId, next.Interval, next.EaseFactor, next.Repetitions, next.NextReviewAt);
                }

                // Check mastered/regression
                await CheckAndUpdateTestStatusAsync(testId, userId, test.Status);

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
            catch (Exception ex) { _logger.LogError(ex, "SubmitDailyAnswers failed for test {TestId}", testId); return Fail(500, "Failed"); }
        }

        public async Task<ResultOptions> UpdateTestStatusAsync(int testId, int knowledgeId, int userId, string status)
        {
            try
            {
                if (status != "inactive" && status != "learning")
                    return Fail(400, "Status must be 'inactive' or 'learning'");

                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                await _repo.UpdateTestStatusAsync(testId, status);
                return Ok(new { testId, status });
            }
            catch (Exception ex) { _logger.LogError(ex, "UpdateTestStatus failed for {TestId}", testId); return Fail(500, "Failed"); }
        }

        /// <summary>Auto-promote learning→mastered or regress mastered→learning.</summary>
        private async Task CheckAndUpdateTestStatusAsync(int testId, int userId, string? currentStatus)
        {
            try
            {
                var groups = await _repo.GetRecentSubmissionGroupsAsync(testId, userId, 5);
                var sessions = groups.Select(g => (g.AvgPoint, g.AvgSpeedRatio)).ToList();

                if (currentStatus == "learning" && SpacedRepetitionEngine.ShouldPromoteToMastered(sessions))
                    await _repo.UpdateTestStatusAsync(testId, "mastered");
                else if (currentStatus == "mastered" && SpacedRepetitionEngine.ShouldRegressToLearning(sessions))
                    await _repo.UpdateTestStatusAsync(testId, "learning");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckAndUpdateTestStatus failed for test {TestId}", testId);
            }
        }

        public async Task<KRetentionSummaryResponse> GetRetentionSummaryAsync(int knowledgeId)
        {
            try
            {
                var testsWithQuestions = await _repo.GetTestsWithQuestionsAsync(knowledgeId);

                var testItems = testsWithQuestions.Select(tw =>
                {
                    var retentions = tw.Questions
                        .Select(q => SpacedRepetitionEngine.CalculateRetention(q.SrsInterval, q.SrsNextReviewAt))
                        .ToList();

                    return new KRetentionTestItem
                    {
                        TestId        = tw.Test.Id,
                        Title         = tw.Test.Title,
                        Retention     = retentions.Count > 0 ? Math.Round(retentions.Average(), 1) : 0,
                        QuestionCount = retentions.Count,
                    };
                }).ToList();

                var totalQuestions = testItems.Sum(t => t.QuestionCount);
                var avg = totalQuestions > 0
                    ? Math.Round(testItems.Sum(t => t.Retention * t.QuestionCount) / totalQuestions, 1)
                    : 0;

                return new KRetentionSummaryResponse
                {
                    Average        = avg,
                    TotalQuestions = totalQuestions,
                    Tests          = testItems,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRetentionSummary failed for knowledge {KnowledgeId}", knowledgeId);
                return new KRetentionSummaryResponse();
            }
        }

        public async Task<KRetentionGraphResponse> GetRetentionGraphAsync(int knowledgeId, int days)
        {
            try
            {
                var testsWithQuestions = await _repo.GetTestsWithQuestionsAsync(knowledgeId);

                var allQuestions = testsWithQuestions
                    .SelectMany(tw => tw.Questions.Select(q => new { Question = q, tw.Test.Title }))
                    .ToList();

                if (allQuestions.Count == 0)
                    return new KRetentionGraphResponse();

                var questionIds = allQuestions.Select(x => x.Question.Id).ToList();
                var questions = allQuestions.Select(x => new KRetentionGraphQuestion
                {
                    Id        = x.Question.Id,
                    Name      = x.Question.Name,
                    TestTitle = x.Title,
                }).ToList();

                // Fetch all point history and group by question
                var allHistory = await _repo.GetAllHistoryForQuestionsAsync(questionIds);
                var historyByQuestion = allHistory
                    .Where(h => h.QuestionId.HasValue)
                    .GroupBy(h => h.QuestionId!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderBy(h => h.CreatedAt).ToList());

                // Replay SM-2 for each question → build list of (reviewDate, interval) segments
                // A segment means: at reviewDate, retention = 100%, then decays with R = 0.9^(daysSince/interval)
                var segmentsByQuestion = new Dictionary<int, List<(DateTime ReviewDate, int Interval)>>();

                foreach (var q in allQuestions)
                {
                    var segments = new List<(DateTime, int)>();
                    var srs = new SpacedRepetitionEngine.SrsState(0, 2.5, 0, null);

                    if (historyByQuestion.TryGetValue(q.Question.Id, out var history))
                    {
                        foreach (var h in history)
                        {
                            srs = SpacedRepetitionEngine.CalculateNext(h.Point, srs);
                            // reviewDate = the moment they answered
                            segments.Add((h.CreatedAt, srs.Interval));
                        }
                    }

                    segmentsByQuestion[q.Question.Id] = segments;
                }

                // Build daily retention values
                var today    = DateTime.UtcNow.Date;
                var daysList = new List<KRetentionGraphDay>();

                for (var d = days - 1; d >= 0; d--)
                {
                    var date = d == 0 ? DateTime.UtcNow : today.AddDays(-d).AddHours(23).AddMinutes(59);

                    var retentions = allQuestions.Select(x =>
                    {
                        var segments = segmentsByQuestion[x.Question.Id];
                        if (segments.Count == 0) return 0.0;

                        // Find the last review that happened before or at `date`
                        (DateTime ReviewDate, int Interval)? activeSegment = null;
                        for (var i = segments.Count - 1; i >= 0; i--)
                        {
                            if (segments[i].ReviewDate <= date)
                            {
                                activeSegment = segments[i];
                                break;
                            }
                        }

                        if (activeSegment == null) return 0.0; // not yet reviewed at this date

                        var interval = activeSegment.Value.Interval;
                        if (interval <= 0) interval = 1; // fail → treat as 1-day for decay
                        var daysSince = (date - activeSegment.Value.ReviewDate).TotalDays;
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

                return new KRetentionGraphResponse { Questions = questions, Days = daysList };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRetentionGraph failed for knowledge {KnowledgeId}", knowledgeId);
                return new KRetentionGraphResponse();
            }
        }

        private static ResultOptions Ok(object data)                  => new() { Success = true,  Object = data, Status = 200 };
        private static ResultOptions Fail(int status, string message) => new() { Success = false, Message = message, Status = status };
    }
}
