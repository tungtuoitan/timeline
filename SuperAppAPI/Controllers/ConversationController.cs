using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _service;
        private readonly ILogger<ConversationController> _logger;

        public ConversationController(IConversationService service, ILogger<ConversationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? GetUserId()
        {
            var claim = User.GetUserId();
            return int.TryParse(claim, out var id) ? id : null;
        }

        // ── Topics ────────────────────────────────────────────────────────────

        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics(
            [FromQuery] string? entityType = null,
            [FromQuery] int? entityId = null,
            [FromQuery] string? deletedAt = "null")
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _service.GetTopicsAsync(new TopicFilterOptions
            {
                UserId = userId.Value,
                EntityType = entityType,
                EntityId = entityId,
                DeletedAt = deletedAt,
            });
            return Ok(result);
        }

        [HttpPost("topics")]
        public async Task<IActionResult> UpsertTopic([FromBody] UpsertTopicRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            request.UserId = userId.Value;
            var result = await _service.UpsertTopicAsync(request);
            return Ok(result);
        }

        [HttpDelete("topics/{id}")]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _service.UpsertTopicAsync(new UpsertTopicRequest
            {
                Id = id,
                UserId = userId.Value,
                Name = "deleted",
                DeletedAt = DateTime.UtcNow,
            });
            return Ok(result);
        }

        // ── Messages ──────────────────────────────────────────────────────────

        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages(
            [FromQuery] string? entityType = null,
            [FromQuery] int? entityId = null,
            [FromQuery] int? topicId = null,
            [FromQuery] bool entityLevelOnly = false,
            [FromQuery] string? deletedAt = "null")
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _service.GetMessagesAsync(new MessageFilterOptions
            {
                UserId = userId.Value,
                EntityType = entityType,
                EntityId = entityId,
                TopicId = topicId,
                EntityLevelOnly = entityLevelOnly,
                DeletedAt = deletedAt,
            });
            return Ok(result);
        }

        [HttpPost("messages")]
        public async Task<IActionResult> UpsertMessage([FromBody] UpsertMessageRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            request.UserId = userId.Value;
            var result = await _service.UpsertMessageAsync(request);
            return Ok(result);
        }

        [HttpDelete("messages/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _service.UpsertMessageAsync(new UpsertMessageRequest
            {
                Id = id,
                UserId = userId.Value,
                Content = null,
                DeletedAt = DateTime.UtcNow,
            });
            return Ok(result);
        }

        // ── Promote message → topic ───────────────────────────────────────────

        [HttpPost("messages/{id}/promote")]
        public async Task<IActionResult> PromoteToTopic(int id, [FromBody] PromoteToTopicRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _service.PromoteMessageToTopicAsync(id, userId.Value, request.TopicName);
            return Ok(result);
        }
    }

    public class PromoteToTopicRequest
    {
        public string TopicName { get; set; } = string.Empty;
    }
}
