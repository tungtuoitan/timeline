using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuperAppAPI.Controllers.Infrastructure
{
    /// <summary>
    /// Diagnostic Controller
    /// Accepts fire-and-forget log events from the frontend for debugging production issues.
    /// All endpoints are anonymous - must work without a token.
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticController : ControllerBase
    {
        private readonly ILogger<DiagnosticController> _logger;

        public DiagnosticController(ILogger<DiagnosticController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Receive a single diagnostic event from the frontend.
        /// POST /api/diagnostic/log
        /// </summary>
        [HttpPost("log")]
        public IActionResult Log([FromBody] DiagnosticLogRequest? request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation(
                "FE_DIAGNOSTIC Category={Category} Event={Event} Data={Data} " +
                "WindowOrigin={WindowOrigin} WindowHref={WindowHref} " +
                "UserAgent={UserAgent} Platform={Platform} ScreenSize={ScreenSize} " +
                "ClientTimestamp={ClientTimestamp} ServerTimestamp={ServerTimestamp} ClientIp={ClientIp}",
                request?.Category, request?.Event, request?.Data,
                request?.WindowOrigin, request?.WindowHref,
                request?.UserAgent, request?.Platform, request?.ScreenSize,
                request?.ClientTimestamp, DateTime.UtcNow.ToString("O"), clientIp);
            return Ok();
        }

        /// <summary>
        /// Receive a batch of debug log entries from the frontend store.
        /// POST /api/diagnostic/logs
        /// </summary>
        [HttpPost("logs")]
        public IActionResult Logs([FromBody] DiagnosticBatchRequest? request)
        {
            if (request?.Entries == null || request.Entries.Count == 0)
                return Ok();

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var serverTs = DateTime.UtcNow.ToString("O");

            foreach (var entry in request.Entries)
            {
                _logger.LogInformation(
                    "FE_LOG #{Id} Category={Category} Event={Event} Data={Data} " +
                    "Origin={WindowOrigin} Href={WindowHref} UA={UserAgent} " +
                    "ClientTs={ClientTimestamp} ServerTs={ServerTimestamp} Ip={ClientIp}",
                    entry.Id, entry.Category, entry.Event, entry.Data != null ? System.Text.Json.JsonSerializer.Serialize(entry.Data) : null,
                    entry.WindowOrigin, entry.WindowHref, entry.UserAgent,
                    entry.Timestamp, serverTs, clientIp);
            }

            return Ok();
        }
    }

    public record DiagnosticLogRequest(
        string? Category,
        string? Event,
        string? Data,
        string? WindowOrigin,
        string? WindowHref,
        string? UserAgent,
        string? Platform,
        string? ScreenSize,
        string? ClientTimestamp
    );

    public record DiagnosticBatchRequest(List<DiagnosticEntryDto> Entries);

    public record DiagnosticEntryDto(
        int Id,
        string Timestamp,
        string Category,
        string Event,
        Dictionary<string, object>? Data,
        string WindowOrigin,
        string WindowHref,
        string UserAgent
    );
}
