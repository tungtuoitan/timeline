using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Controllers;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.DailyLog;
using SuperAppServices.Interfaces.DailyLog;

namespace SuperAppAPI.Controllers.DailyLog
{
    [ApiController]
    [Route("api/daily-log-template")]
    [Authorize]
    public class DailyLogTemplateController : BaseAuthController
    {
        private readonly IDailyLogTemplateService _service;
        private readonly ILogger<DailyLogTemplateController> _logger;

        public DailyLogTemplateController(IDailyLogTemplateService service, ILogger<DailyLogTemplateController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplate()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var response = await _service.GetTemplateAsync(userId.Value);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertTemplate([FromBody] List<UpsertDailyLogTemplateRequest> requests)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (requests == null || requests.Count == 0) return BadRequest("At least one field is required");

            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Upserting daily-log template userId={UserId} count={Count}", userId.Value, requests.Count);
            var response = await _service.UpsertTemplateAsync(userId.Value, requests);
            return Ok(response);
        }
    }
}
