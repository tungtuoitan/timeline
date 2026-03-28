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

        // GET /api/k/{knowledgeId}/tests
        [HttpGet("tests")]
        public async Task<IActionResult> GetTests(int knowledgeId)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                return Ok(await _service.GetTestsAsync(knowledgeId, userId.Value));
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

        // POST /api/k/{knowledgeId}/tests/create
        [HttpPost("tests/create")]
        public async Task<IActionResult> CreateTestFromNodes(int knowledgeId, [FromBody] KCreateTestFromNodesRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.CreateTestFromNodesAsync(knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while creating the test", Status = 500 });
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

        // GET /api/k/{knowledgeId}/node-scores
        [HttpGet("node-scores")]
        public async Task<IActionResult> GetNodeScores(int knowledgeId)
        {
            try
            {
                var userId = UserId;
                if (userId == null) return Unauthorized();
                return Ok(await _service.GetNodeScoresAsync(knowledgeId, userId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node scores for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while retrieving node scores", Status = 500 });
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

        // PATCH /api/k/{knowledgeId}/tests/{testId}/nodes
        [HttpPatch("tests/{testId:int}/nodes")]
        public async Task<IActionResult> UpdateTestNodes(int knowledgeId, int testId, [FromBody] KUpdateTestNodesRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = UserId;
                if (userId == null) return Unauthorized();
                var result = await _service.UpdateTestNodesAsync(testId, knowledgeId, userId.Value, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test nodes for test {TestId}, knowledge {KnowledgeId}", testId, knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while updating test nodes", Status = 500 });
            }
        }
    }
}
