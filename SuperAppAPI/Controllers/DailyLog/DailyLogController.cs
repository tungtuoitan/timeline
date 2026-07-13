using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Controllers;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.DailyLog;
using SuperAppServices.Interfaces.DailyLog;

namespace SuperAppAPI.Controllers.DailyLog
{
    [ApiController]
    [Route("api/daily-log")]
    [Authorize]
    public class DailyLogController : BaseAuthController
    {
        private readonly IDailyLogService _service;
        private readonly ILogger<DailyLogController> _logger;

        public DailyLogController(IDailyLogService service, ILogger<DailyLogController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] string? deletedAt = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var filter = new DailyLogFilterOptions
            {
                UserId = userId.Value,
                FromDate = ParseDate(from),
                ToDate = ParseDate(to),
                DeletedAt = deletedAt
            };

            _logger.LogInformation("Retrieving daily logs for userId={UserId} from={From} to={To}", userId.Value, from, to);
            var response = await _service.GetLogsAsync(filter);
            return Ok(response);
        }

        [HttpGet("{date}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogByDate(string date)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            var parsed = ParseDate(date);
            if (parsed == null) return BadRequest("Invalid date format. Expected yyyy-MM-dd.");

            var response = await _service.GetLogByDateAsync(userId.Value, parsed.Value);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertLog([FromBody] UpsertDailyLogRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            request.UserId = userId.Value;

            _logger.LogInformation("Upserting daily log userId={UserId} date={Date}", userId.Value, request.LogDate);
            var response = await _service.UpsertLogAsync(request);
            return Ok(response);
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFieldHistory(
            [FromQuery] string fieldKey,
            [FromQuery] string? from = null,
            [FromQuery] string? to = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            if (string.IsNullOrWhiteSpace(fieldKey)) return BadRequest("fieldKey is required");

            var response = await _service.GetFieldHistoryAsync(userId.Value, fieldKey, ParseDate(from), ParseDate(to));
            return Ok(response);
        }

        private static DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var d)) return d.Date;
            return null;
        }
    }
}
