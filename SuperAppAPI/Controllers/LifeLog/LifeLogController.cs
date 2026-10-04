using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;
using SuperAppModels.Time;

namespace SuperAppAPI.Controllers.LifeLog
{
    /// <summary>
    /// Controller for LifeLog feature - tracks and logs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LifeLogController : BaseAuthController
    {
        private readonly ILifeLogService _service;
        private readonly ILogger<LifeLogController> _logger;

        public LifeLogController(ILifeLogService service, ILogger<LifeLogController> logger)
        {
            _service = service;
            _logger = logger;
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
            var userId = GetUserId();
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

        [HttpGet("tracks/{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrackById(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            var response = await _service.GetTrackByIdAsync(id, userId.Value);
            return Ok(response);
        }

        [HttpPost("tracks/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertTracks([FromBody] List<UpsertLifeLogTrackRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("UpsertTracks ModelState invalid: {Errors}", string.Join("; ", errors));
                return BadRequest(ModelState);
            }
            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("UpsertTracks called with empty body");
                return BadRequest("At least one track is required");
            }

            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            foreach (var r in requests) r.UserId = userId.Value;

            _logger.LogInformation("UpsertTracks {Count} for userId: {UserId} | data: {Data}",
                requests.Count, userId,
                string.Join("; ", requests.Select(r => $"[id={r.Id} name={r.Name} emoji={r.Emoji} color={r.Color} isSensitive={r.IsSensitive} deletedAt={r.DeletedAt}]")));

            var response = await _service.UpsertTracksAsync(requests);

            _logger.LogInformation("UpsertTracks result: success={Success} message={Message} count={Count}",
                response.Success, response.Message, response.Data?.Count ?? 0);

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
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var filter = new LifeLogLogFilterOptions
            {
                UserId = userId.Value,
                SearchText = searchText,
                Types = !string.IsNullOrEmpty(type)
                    ? type.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : null,
                TrackId = trackId,
                CreatedFrom = TimeParsing.ParseInstantLenient(createdAtFrom),
                CreatedTo = TimeParsing.ParseInstantLenient(createdAtTo),
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

        [HttpGet("logs/{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogById(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            var response = await _service.GetLogByIdAsync(id, userId.Value);
            return Ok(response);
        }

        [HttpPost("logs/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertLogs([FromBody] List<UpsertLifeLogLogRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("UpsertLogs ModelState invalid: {Errors}", string.Join("; ", errors));
                return BadRequest(ModelState);
            }
            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("UpsertLogs called with empty body");
                return BadRequest("At least one log is required");
            }

            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            foreach (var r in requests) r.UserId = userId.Value;

            _logger.LogInformation("UpsertLogs {Count} for userId: {UserId} | data: {Data}",
                requests.Count, userId,
                string.Join("; ", requests.Select(r => $"[id={r.Id} type={r.Type} trackId={r.TrackId} title={r.Title} isSensitive={r.IsSensitive} occurAt={r.OccurAt} deletedAt={r.DeletedAt}]")));

            var response = await _service.UpsertLogsAsync(requests);

            _logger.LogInformation("UpsertLogs result: success={Success} message={Message} count={Count}",
                response.Success, response.Message, response.Data?.Count ?? 0);

            return Ok(response);
        }
    }
}
