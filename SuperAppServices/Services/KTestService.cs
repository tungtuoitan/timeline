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

        public async Task<List<KTestSummaryResponse>> GetTestsAsync(int knowledgeId, int userId)
        {
            try   { return await _repo.GetTestSummariesAsync(knowledgeId, userId); }
            catch (Exception ex) { _logger.LogError(ex, "GetTests failed"); return []; }
        }

        public async Task<ResultOptions> GetTestDetailAsync(int testId, int knowledgeId, int userId)
        {
            try
            {
                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                var testNodes     = test.TestNodes.ToList();
                var nodeIds       = testNodes.Select(tn => tn.NodeId).ToList();
                var questionNodes = await _repo.GetQuestionNodesByIdsAsync(nodeIds);
                var nodeMap       = questionNodes.ToDictionary(n => n.Id);

                // Per-node score history: last ≤10 points (0–5), oldest→newest
                var allHistory = await _repo.GetHistoryForNodesAsync(test.Id, userId, nodeIds);
                var historyByNode = allHistory
                    .GroupBy(h => h.NodeId)
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
                    Questions   = testNodes
                        .Where(tn => nodeMap.ContainsKey(tn.NodeId))
                        .Select(tn =>
                        {
                            var node = nodeMap[tn.NodeId];
                            return new KTestQuestionResponse
                            {
                                TestNodeId   = tn.Id,
                                NodeId       = node.Id,
                                Question     = node.Name,
                                Answer       = node.Description,
                                IsActive     = tn.IsActive,
                                ScoreHistory = historyByNode.TryGetValue(node.Id, out var hist) ? hist : [],
                            };
                        }).ToList(),
                    CreatedAt   = test.CreatedAt,
                });
            }
            catch (Exception ex) { _logger.LogError(ex, "GetTestDetail failed for {TestId}", testId); return Fail(500, "Failed"); }
        }

        public async Task<ResultOptions> CreateTestFromNodesAsync(int knowledgeId, int userId, KCreateTestFromNodesRequest request)
        {
            try
            {
                var count         = Math.Clamp(request.Count, 1, 100);
                var questionNodes = await _repo.GetQuestionNodesAsync(knowledgeId, request.NodeIds, request.IncludeDescendants);
                if (!questionNodes.Any()) return Fail(400, "Không tìm thấy câu hỏi nào dưới các node đã chọn.");

                var sampled = questionNodes.OrderBy(_ => Guid.NewGuid()).Take(count).ToList();
                var nodeIds = sampled.Select(n => n.Id).ToList();

                var test = new KTestEntity
                {
                    KnowledgeId = knowledgeId,
                    UserId      = userId,
                    Title       = request.Title,
                    Level       = request.Level,
                    Mode        = "standard",
                };

                var created = await _repo.CreateTestAsync(test, nodeIds);

                return Ok(new KTestDetailResponse
                {
                    Id          = created.Id,
                    KnowledgeId = created.KnowledgeId,
                    Title       = created.Title,
                    Level       = created.Level,
                    Mode        = created.Mode,
                    Questions   = sampled.Select(n => new KTestQuestionResponse { NodeId = n.Id, Question = n.Name }).ToList(),
                    CreatedAt   = created.CreatedAt,
                });
            }
            catch (Exception ex) { _logger.LogError(ex, "CreateTestFromNodes failed"); return Fail(500, "Failed to create test"); }
        }

        public async Task<ResultOptions> SubmitAnswersAsync(int testId, int knowledgeId, int userId, KSubmitAnswersRequest request)
        {
            try
            {
                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                if (request.Answers == null || !request.Answers.Any())
                    return Fail(400, "Answers are required");

                var nodeIds       = request.Answers.Select(a => a.NodeId).Distinct().ToList();
                var questionNodes = await _repo.GetQuestionNodesByIdsAsync(nodeIds);
                var nodeMap       = questionNodes.ToDictionary(n => n.Id);

                var submissions = request.Answers.Select(a => (a.NodeId, a.AnswerText)).ToList();
                var grading     = await _grading.GradeSubmissionAsync(test.Title, submissions, questionNodes);

                var results = grading.Answers.Select(g => (
                    g.NodeId,
                    request.Answers.FirstOrDefault(a => a.NodeId == g.NodeId)?.AnswerText,
                    g.Point
                )).ToList();

                await _repo.SaveSubmissionAsync(testId, userId, results);

                var pct = grading.MaxPoints > 0
                    ? (int)Math.Round((double)grading.TotalPoints / grading.MaxPoints * 100) : 0;

                return Ok(new KSubmitAnswersResultResponse
                {
                    TotalPoints = grading.TotalPoints,
                    MaxPoints   = grading.MaxPoints,
                    Pct         = pct,
                    Grades      = grading.Answers.Select(g =>
                    {
                        nodeMap.TryGetValue(g.NodeId, out var node);
                        return new KNodeGradeResponse
                        {
                            NodeId         = g.NodeId,
                            Question       = node?.Name ?? "",
                            AnswerText     = request.Answers.FirstOrDefault(a => a.NodeId == g.NodeId)?.AnswerText,
                            ExpectedAnswer = node?.Description,
                            Point          = g.Point,
                            Comment        = g.Comment,
                        };
                    }).ToList(),
                });
            }
            catch (Exception ex) { _logger.LogError(ex, "SubmitAnswers failed for test {TestId}", testId); return Fail(500, "Failed to submit answers"); }
        }

        public async Task<Dictionary<int, int>> GetNodeScoresAsync(int knowledgeId, int userId)
        {
            try   { return await _repo.GetNodeScoresAsync(knowledgeId, userId); }
            catch (Exception ex) { _logger.LogError(ex, "GetNodeScores failed"); return []; }
        }

        public async Task<ResultOptions> UpdateTestAsync(int testId, int knowledgeId, int userId, KUpdateTestRequest request)
        {
            try
            {
                var updated = await _repo.UpdateTestTitleAsync(testId, knowledgeId, request.Title);
                if (updated == null) return Fail(404, "Test not found");
                return Ok(new { updated.Id, updated.Title });
            }
            catch (Exception ex) { _logger.LogError(ex, "UpdateTest failed for {TestId}", testId); return Fail(500, "Failed to update test"); }
        }

        public async Task<ResultOptions> UpdateTestNodesAsync(int testId, int knowledgeId, int userId, KUpdateTestNodesRequest request)
        {
            try
            {
                var test = await _repo.GetTestByIdAsync(testId, knowledgeId);
                if (test == null) return Fail(404, "Test not found");

                if (request.AddNodeIds.Count > 0)
                    await _repo.AddTestNodesAsync(testId, request.AddNodeIds);

                if (request.ToggleTestNodeIds.Count > 0)
                    await _repo.ToggleTestNodesActiveAsync(request.ToggleTestNodeIds);

                if (request.DeleteTestNodeIds.Count > 0)
                    await _repo.DeleteTestNodesAsync(request.DeleteTestNodeIds);

                return Ok(new { testId });
            }
            catch (Exception ex) { _logger.LogError(ex, "UpdateTestNodes failed for {TestId}", testId); return Fail(500, "Failed to update test nodes"); }
        }

        private static ResultOptions Ok(object data)                  => new() { Success = true,  Object = data, Status = 200 };
        private static ResultOptions Fail(int status, string message) => new() { Success = false, Message = message, Status = status };
    }
}
