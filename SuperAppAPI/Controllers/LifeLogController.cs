using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for LifeLog feature - tracks and logs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LifeLogController : ControllerBase
    {
        private readonly ILifeLogService _service;
        private readonly ILogger<LifeLogController> _logger;

        public LifeLogController(ILifeLogService service, ILogger<LifeLogController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? GetAuthenticatedUserId()
        {
            var claim = User.GetUserId();
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId)) return null;
            return userId;
        }

        // ─── TRACKS ────────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/lifelog/tracks - get user's tracks
        /// </summary>
        [HttpGet("tracks")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTracks(
            [FromQuery] string? searchText = null,
            [FromQuery] string? deletedAt = null,
            [FromQuery] string? ids = null)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var filter = new LifeLogTrackFilterOptions
            {
                UserId = userId.Value,
                SearchText = searchText,
                DeletedAt = deletedAt,
                Ids = !string.IsNullOrEmpty(ids)
                    ? ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                        .Where(id => id > 0).ToList()
                    : null
            };

            _logger.LogInformation("GetTracks for userId: {UserId}", userId);
            var response = await _service.GetTracksAsync(filter);
            return Ok(response);
        }

        /// <summary>
        /// POST /api/lifelog/tracks/batch - create or update tracks
        /// </summary>
        [HttpPost("tracks/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertTracks([FromBody] List<UpsertLifeLogTrackRequest> requests)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (requests == null || !requests.Any()) return BadRequest("At least one track is required");

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            foreach (var r in requests) r.UserId = userId.Value;

            _logger.LogInformation("UpsertTracks {Count} for userId: {UserId}", requests.Count, userId);
            var response = await _service.UpsertTracksAsync(requests);
            return Ok(response);
        }

        // ─── LOGS ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/lifelog/logs - get user's log entries
        /// </summary>
        [HttpGet("logs")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? searchText = null,
            [FromQuery] string? type = null,
            [FromQuery] int? trackId = null,
            [FromQuery] string? createdAtFrom = null,
            [FromQuery] string? createdAtTo = null,
            [FromQuery] string? deletedAt = null,
            [FromQuery] string? ids = null)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var filter = new LifeLogLogFilterOptions
            {
                UserId = userId.Value,
                SearchText = searchText,
                Types = !string.IsNullOrEmpty(type)
                    ? type.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : null,
                TrackId = trackId,
                CreatedFrom = !string.IsNullOrEmpty(createdAtFrom) && DateTime.TryParse(createdAtFrom, out var from) ? from : null,
                CreatedTo = !string.IsNullOrEmpty(createdAtTo) && DateTime.TryParse(createdAtTo, out var to) ? to : null,
                DeletedAt = deletedAt,
                Ids = !string.IsNullOrEmpty(ids)
                    ? ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                        .Where(id => id > 0).ToList()
                    : null
            };

            _logger.LogInformation("GetLogs for userId: {UserId}", userId);
            var response = await _service.GetLogsAsync(filter);
            return Ok(response);
        }

        /// <summary>
        /// POST /api/lifelog/logs/batch - create or update log entries
        /// </summary>
        [HttpPost("logs/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertLogs([FromBody] List<UpsertLifeLogLogRequest> requests)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (requests == null || !requests.Any()) return BadRequest("At least one log is required");

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            foreach (var r in requests) r.UserId = userId.Value;

            _logger.LogInformation("UpsertLogs {Count} for userId: {UserId}", requests.Count, userId);
            var response = await _service.UpsertLogsAsync(requests);
            return Ok(response);
        }
    }
}
