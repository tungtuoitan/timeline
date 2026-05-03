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
    public class KQuestionController : ControllerBase
    {
        private readonly IKQuestionService _service;
        private readonly ILogger<KQuestionController> _logger;

        public KQuestionController(IKQuestionService service, ILogger<KQuestionController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        private int? UserId => int.TryParse(User.GetUserId(), out var id) ? id : null;

        // GET /api/k/global-daily-queue
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

        // GET /api/k/{knowledgeId}/questions
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions(int knowledgeId)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.GetQuestionsAsync(knowledgeId, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // PATCH /api/k/{knowledgeId}/questions
        [HttpPatch("questions")]
        public async Task<IActionResult> UpdateQuestions(int knowledgeId, [FromBody] KUpdateQuestionsRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.UpdateQuestionsAsync(knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating questions for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // POST /api/k/{knowledgeId}/questions/submit
        [HttpPost("questions/submit")]
        public async Task<IActionResult> SubmitAnswers(int knowledgeId, [FromBody] KSubmitAnswersRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.SubmitAnswersAsync(knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answers for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
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
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

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

        // GET /api/k/{knowledgeId}/daily-session?limit=30
        [HttpGet("daily-session")]
        public async Task<IActionResult> GetDailySession(int knowledgeId, [FromQuery] int limit = 30)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.GetDailySessionAsync(knowledgeId, userId.Value, limit);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily session for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // POST /api/k/{knowledgeId}/daily-submit
        [HttpPost("daily-submit")]
        public async Task<IActionResult> SubmitDailyAnswers(int knowledgeId, [FromBody] KDailySubmitRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.SubmitDailyAnswersAsync(knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting daily answers for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{knowledgeId}/retention
        [HttpGet("retention")]
        public async Task<IActionResult> GetRetention(int knowledgeId)
        {
            try
            {
                var result = await _service.GetRetentionSummaryAsync(knowledgeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retention for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{knowledgeId}/retention-graph?days=14
        [HttpGet("retention-graph")]
        public async Task<IActionResult> GetRetentionGraph(int knowledgeId, [FromQuery] int days = 14)
        {
            try
            {
                if (days < 1 || days > 90) days = 14;
                var result = await _service.GetRetentionGraphAsync(knowledgeId, days);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retention graph for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }
    }
}
