using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.K
{
    [ApiController]
    [Route("api/k/{nodeId:int}")]
    [Authorize]
    public class KQuestionController : BaseAuthController
    {
        private readonly IKQuestionService _service;
        private readonly ILogger<KQuestionController> _logger;

        public KQuestionController(IKQuestionService service, ILogger<KQuestionController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        // GET /api/k/global-daily-queue
        //[HttpGet("/api/k/global-daily-queue")]
        //public async Task<IActionResult> GetGlobalDailyQueue()
        //{
        //    try
        //    {
        //        var userId = GetUserId();
        //        if (userId == null) return Unauthorized();
        //        var result = await _service.GetGlobalDailyQueueAsync(userId.Value);
        //        return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting global daily queue");
        //        return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
        //    }
        //}

        // GET /api/k/{nodeId}/node-questions
        [HttpGet("node-questions")]
        public async Task<IActionResult> GetNodeQuestions(int nodeId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.GetNodeQuestionsAsync(nodeId, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node questions for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{knowledgeId}/questions  (knowledge-level: all questions across all nodes in a knowledge)
        [HttpGet("questions")]
        public async Task<IActionResult> GetKnowledgeQuestions(int nodeId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.GetKnowledgeQuestionsAsync(nodeId, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge questions for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // PATCH /api/k/{nodeId}/questions
        [HttpPatch("questions")]
        public async Task<IActionResult> UpdateQuestions(int nodeId, [FromBody] KUpdateQuestionsRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.UpdateQuestionsAsync(nodeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating questions for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // POST /api/k/{nodeId}/questions/submit
        [HttpPost("questions/submit")]
        public async Task<IActionResult> SubmitAnswers(int nodeId, [FromBody] KSubmitAnswersRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.SubmitAnswersAsync(nodeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answers for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{nodeId}/question-scores
        [HttpGet("question-scores")]
        public async Task<IActionResult> GetQuestionScores(int nodeId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                return Ok(await _service.GetQuestionScoresAsync(nodeId, userId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question scores for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // PATCH /api/k/{nodeId}/questions/{questionId}/mark-draft
        [HttpPatch("questions/{questionId:int}/mark-draft")]
        public async Task<IActionResult> MarkQuestionDraft(int nodeId, int questionId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.MarkQuestionDraftAsync(questionId);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking question {QuestionId} as draft", questionId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{nodeId}/daily-queue
        [HttpGet("daily-queue")]
        public async Task<IActionResult> GetDailyQueue(int nodeId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.GetDailyQueueAsync(nodeId, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily queue for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{nodeId}/daily-session?limit=30
        [HttpGet("daily-session")]
        public async Task<IActionResult> GetDailySession(int nodeId, [FromQuery] int limit = 30)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.GetDailySessionAsync(nodeId, userId.Value, limit);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily session for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{nodeId}/knowledge-daily-session?limit=30
        [HttpGet("knowledge-daily-session")]
        public async Task<IActionResult> GetKnowledgeDailySession(int nodeId, [FromQuery] int limit = 30)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.GetKnowledgeDailySessionAsync(nodeId, userId.Value, limit);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge daily session for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // POST /api/k/{nodeId}/daily-submit
        [HttpPost("daily-submit")]
        public async Task<IActionResult> SubmitDailyAnswers(int nodeId, [FromBody] KDailySubmitRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.SubmitDailyAnswersAsync(nodeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting daily answers for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{nodeId}/retention
        [HttpGet("retention")]
        public async Task<IActionResult> GetRetention(int nodeId)
        {
            try
            {
                var result = await _service.GetRetentionSummaryAsync(nodeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retention for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{nodeId}/retention-graph?days=14
        [HttpGet("retention-graph")]
        public async Task<IActionResult> GetRetentionGraph(int nodeId, [FromQuery] int days = 14)
        {
            try
            {
                if (days < 1 || days > 90) days = 14;
                var result = await _service.GetRetentionGraphAsync(nodeId, days);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retention graph for node {NodeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // GET /api/k/{knowledgeId}/question-status-timeline
        [HttpGet("question-status-timeline")]
        public async Task<IActionResult> GetQuestionStatusTimeline(int nodeId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _service.GetQuestionStatusTimelineAsync(nodeId, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question status timeline for knowledge {KnowledgeId}", nodeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }
    }
}
