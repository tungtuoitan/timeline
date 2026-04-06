using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("api/k/{knowledgeId:int}")]
    [Authorize]
    public class KTestController : ControllerBase
    {
        private readonly IKTestService _service;
        private readonly ILogger<KTestController> _logger;

        public KTestController(IKTestService service, ILogger<KTestController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        private int? UserId => int.TryParse(User.GetUserId(), out var id) ? id : null;

        // GET /api/k/global-daily-queue (static segment before {knowledgeId} template)
        [HttpGet("/api/k/global-daily-queue")]
        public async Task<IActionResult> GetGlobalDailyQueue()
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.GetGlobalDailyQueueAsync(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting global daily queue");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{knowledgeId}/tests?nodeId={nodeId}
        [HttpGet("tests")]
        public async Task<IActionResult> GetTests(int knowledgeId, [FromQuery] int? nodeId = null)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                return Ok(await _service.GetTestsAsync(knowledgeId, userId.Value, nodeId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tests for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while retrieving tests", Status = 500 });
            }
        }

        // GET /api/k/{knowledgeId}/tests/{testId}
        [HttpGet("tests/{testId:int}")]
        public async Task<IActionResult> GetTestDetail(int knowledgeId, int testId)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.GetTestDetailAsync(testId, knowledgeId, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test detail for test {TestId}, knowledge {KnowledgeId}", testId, knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while retrieving test detail", Status = 500 });
            }
        }

        // POST /api/k/{knowledgeId}/tests/create-empty
        [HttpPost("tests/create-empty")]
        public async Task<IActionResult> CreateEmptyTest(int knowledgeId, [FromBody] KCreateEmptyTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.CreateEmptyTestAsync(knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating empty test for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // POST /api/k/{knowledgeId}/tests/{testId}/submit
        [HttpPost("tests/{testId:int}/submit")]
        public async Task<IActionResult> SubmitAnswers(int knowledgeId, int testId, [FromBody] KSubmitAnswersRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.SubmitAnswersAsync(testId, knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answers for test {TestId}, knowledge {KnowledgeId}", testId, knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while submitting answers", Status = 500 });
            }
        }

        // GET /api/k/{knowledgeId}/question-scores
        [HttpGet("question-scores")]
        public async Task<IActionResult> GetQuestionScores(int knowledgeId)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                return Ok(await _service.GetQuestionScoresAsync(knowledgeId, userId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question scores for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while retrieving question scores", Status = 500 });
            }
        }

        // PUT /api/k/{knowledgeId}/tests/reorder
        [HttpPut("tests/reorder")]
        public async Task<IActionResult> ReorderTests(int knowledgeId, [FromBody] List<int> orderedTestIds)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.ReorderTestsAsync(knowledgeId, userId.Value, orderedTestIds);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering tests for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // PUT /api/k/{knowledgeId}/tests/{testId}
        [HttpPut("tests/{testId:int}")]
        public async Task<IActionResult> UpdateTest(int knowledgeId, int testId, [FromBody] KUpdateTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.UpdateTestAsync(testId, knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test {TestId}, knowledge {KnowledgeId}", testId, knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while updating the test", Status = 500 });
            }
        }

        // PATCH /api/k/{knowledgeId}/tests/{testId}/questions
        [HttpPatch("tests/{testId:int}/questions")]
        public async Task<IActionResult> UpdateQuestions(int knowledgeId, int testId, [FromBody] KUpdateQuestionsRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.UpdateQuestionsAsync(testId, knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating questions for test {TestId}, knowledge {KnowledgeId}", testId, knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while updating questions", Status = 500 });
            }
        }

        // ── SRS / Daily Review ──────────────────────────────────────────────

        // GET /api/k/{knowledgeId}/daily-queue
        [HttpGet("daily-queue")]
        public async Task<IActionResult> GetDailyQueue(int knowledgeId)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.GetDailyQueueAsync(knowledgeId, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily queue for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{knowledgeId}/tests/{testId}/daily-session?limit=30
        [HttpGet("tests/{testId:int}/daily-session")]
        public async Task<IActionResult> GetDailySession(int knowledgeId, int testId, [FromQuery] int limit = 30)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.GetDailySessionAsync(testId, knowledgeId, userId.Value, limit);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily session for test {TestId}", testId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // POST /api/k/{knowledgeId}/tests/{testId}/daily-submit
        [HttpPost("tests/{testId:int}/daily-submit")]
        public async Task<IActionResult> SubmitDailyAnswers(int knowledgeId, int testId, [FromBody] KDailySubmitRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.SubmitDailyAnswersAsync(testId, knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting daily answers for test {TestId}", testId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // PUT /api/k/{knowledgeId}/tests/{testId}/status
        [HttpPut("tests/{testId:int}/status")]
        public async Task<IActionResult> UpdateTestStatus(int knowledgeId, int testId, [FromBody] KUpdateTestStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.UpdateTestStatusAsync(testId, knowledgeId, userId.Value, request.Status);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test status for test {TestId}", testId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }
    }
}
