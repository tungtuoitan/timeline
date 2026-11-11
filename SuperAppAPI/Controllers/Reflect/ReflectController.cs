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

        public ReflectController(IReflectService service)
        {
            _service = service;
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
    }
}
