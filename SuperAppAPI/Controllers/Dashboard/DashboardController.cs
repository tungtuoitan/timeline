using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.Time;
using SuperAppServices.Interfaces.Dashboard;

namespace SuperAppAPI.Controllers.Dashboard
{
    /// <summary>Read-only aggregates for the homepage progress dashboard (TungRoot #1481).</summary>
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : BaseAuthController
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET /api/dashboard/activity?from=yyyy-MM-dd&amp;to=yyyy-MM-dd&amp;types=devlog,comment,decision
        /// → data: [{weekStart, projectId, projectName, projectStatus, count}].
        /// </summary>
        [HttpGet("activity")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActivity([FromQuery] string? from = null, [FromQuery] string? to = null, [FromQuery] string? types = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var result = await _service.GetActivityAsync(userId.Value, TimeParsing.ParseDateOrNull(from), TimeParsing.ParseDateOrNull(to), types);
            return result.Status == 400 ? BadRequest(result) : Ok(result);
        }

        /// <summary>
        /// GET /api/dashboard/habits?from&amp;to&amp;taskIds=1,2&amp;excludeTaskIds=3
        /// → data: [{taskId, title, projectId, status, entries: [{commentId, date, type, content}]}].
        /// </summary>
        [HttpGet("habits")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHabits(
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] string? taskIds = null,
            [FromQuery] string? excludeTaskIds = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var result = await _service.GetHabitsAsync(userId.Value, TimeParsing.ParseDateOrNull(from), TimeParsing.ParseDateOrNull(to), taskIds, excludeTaskIds);
            return result.Status == 400 ? BadRequest(result) : Ok(result);
        }
    }
}
