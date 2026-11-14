using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Reflect;
using SuperAppModels.Time;
using SuperAppServices.Interfaces.Reflect;

namespace SuperAppAPI.Controllers.Reflect
{
    [ApiController]
    [Route("api/reflect")]
    [Authorize]
    public class ReflectController : BaseAuthController
    {
        private readonly IReflectService _service;
        private readonly IReflectScanService _scan;

        public ReflectController(IReflectService service, IReflectScanService scan)
        {
            _service = service;
            _scan = scan;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEntries([FromQuery] string? createdAtFrom = null, [FromQuery] string? createdAtTo = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            // Same as the Workspace filter: "yyyy-MM" / "yyyy-MM-dd" are read in the user's timezone, an ISO instant keeps its offset
            return Ok(await _service.GetEntriesAsync(userId.Value, TimeParsing.ParseInstantLenient(createdAtFrom), TimeParsing.ParseInstantLenientEnd(createdAtTo)));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertEntry([FromBody] UpsertReflectEntryRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            request.UserId = userId.Value;

            return Ok(await _service.UpsertEntryAsync(request));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteEntry(string id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            return Ok(await _service.DeleteEntryAsync(userId.Value, id));
        }

        /// <summary>Daily numbers read out of the journal, one series per metric (the data of the charts)</summary>
        [HttpGet("series")]
        [ProducesResponseType(typeof(List<SuperAppModels.DTOs.Reflect.ReflectSeries>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSeries([FromQuery] string? from = null, [FromQuery] string? to = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            DateTime? fromDay = DateTime.TryParse(from, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var f) ? f : null;
            DateTime? toDay = DateTime.TryParse(to, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t) ? t : null;
            return Ok(await _scan.GetSeriesAsync(userId.Value, fromDay, toDay));
        }

        /// <summary>Reads the rows that are new or edited since their last scan now, instead of waiting for the nightly run</summary>
        [HttpPost("scan")]
        [ProducesResponseType(typeof(SuperAppModels.DTOs.Reflect.ReflectScanResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ScanNow(CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            return Ok(await _scan.ScanUserAsync(userId.Value, ct));
        }
    }
}
