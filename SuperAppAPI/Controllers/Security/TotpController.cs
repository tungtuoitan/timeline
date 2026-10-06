using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Auth;
using SuperAppServices.Interfaces.Auth;

namespace SuperAppAPI.Controllers.Security
{
    /// <summary>
    /// TOTP second factor for the private part of the homepage (TungRoot #1489). Not under /api/auth/
    /// on purpose: the frontend refreshes expired access tokens for every route except /api/auth/*.
    /// </summary>
    [ApiController]
    [Route("api/security/totp")]
    [Authorize]
    public class TotpController : BaseAuthController
    {
        private readonly ITotpService _service;

        public TotpController(ITotpService service)
        {
            _service = service;
        }

        [HttpGet("status")]
        public Task<IActionResult> Status() => Run(uid => _service.GetStatusAsync(uid));

        [HttpPost("setup")]
        public Task<IActionResult> Setup() => Run(uid => _service.SetupAsync(uid));

        [HttpPost("confirm")]
        public Task<IActionResult> Confirm([FromBody] TotpCodeRequest request) => Run(uid => _service.ConfirmAsync(uid, request.Code));

        [HttpPost("unlock")]
        public Task<IActionResult> Unlock([FromBody] TotpCodeRequest request) => Run(uid => _service.UnlockAsync(uid, request.Code));

        [HttpPost("disable")]
        public Task<IActionResult> Disable([FromBody] TotpCodeRequest request) => Run(uid => _service.DisableAsync(uid, request.Code));

        /// <summary>Real HTTP status codes (400/404/409/423) so clients can branch without parsing.</summary>
        private async Task<IActionResult> Run(Func<int, Task<ResultOptions>> action)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            var result = await action(userId.Value);
            return result.Status is >= 200 and < 300 ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }
    }
}
