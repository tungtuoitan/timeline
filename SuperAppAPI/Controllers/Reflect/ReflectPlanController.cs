using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Reflect;
using SuperAppServices.Interfaces.Reflect;

namespace SuperAppAPI.Controllers.Reflect
{
    [ApiController]
    [Route("api/reflect/plan")]
    [Authorize]
    public class ReflectPlanController : BaseAuthController
    {
        private readonly IReflectPlanService _service;

        public ReflectPlanController(IReflectPlanService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetItems([FromQuery] string? from = null, [FromQuery] string? to = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            DateTime? fromDay = DateTime.TryParse(from, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var f) ? f : null;
            DateTime? toDay = DateTime.TryParse(to, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t) ? t : null;
            return Ok(await _service.GetItemsAsync(userId.Value, fromDay, toDay));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertItem([FromBody] UpsertReflectPlanItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            request.UserId = userId.Value;

            return Ok(await _service.UpsertItemAsync(request));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteItem(string id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            return Ok(await _service.DeleteItemAsync(userId.Value, id));
        }
    }
}
