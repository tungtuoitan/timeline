using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for task comment operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaskCommentController : ControllerBase
    {
        private readonly ITaskCommentService _service;
        private readonly ILogger<TaskCommentController> _logger;

        public TaskCommentController(
            ITaskCommentService service,
            ILogger<TaskCommentController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? GetAuthenticatedUserId()
        {
            var userIdClaim = User.GetUserId();
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }

        /// <summary>
        /// Get all comments for a task
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetComments([FromQuery] int taskId)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Getting comments for taskId: {TaskId}, userId: {UserId}", taskId, userId.Value);

            var response = await _service.GetCommentsByTaskIdAsync(taskId, userId.Value);
            return Ok(response);
        }

        /// <summary>
        /// Create or update a comment
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpsertComment([FromBody] UpsertTaskCommentRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Upserting comment for taskId: {TaskId}, userId: {UserId}",
                request.TaskId, userId.Value);

            var response = await _service.UpsertCommentAsync(request, userId.Value);
            return Ok(response);
        }

        /// <summary>
        /// Soft delete a comment and its replies
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Deleting comment ID: {Id}, userId: {UserId}", id, userId.Value);

            var response = await _service.DeleteCommentAsync(id, userId.Value);
            return Ok(response);
        }
    }
}
