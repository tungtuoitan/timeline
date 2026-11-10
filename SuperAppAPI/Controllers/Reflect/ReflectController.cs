using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Reflect;
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
        public async Task<IActionResult> GetEntries()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            return Ok(await _service.GetEntriesAsync(userId.Value));
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
