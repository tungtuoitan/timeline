using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IWebHostEnvironment _env;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IWebHostEnvironment env)
        {
            _authService = authService;
            _logger = logger;
            _env = env;
        }

        // ── Cookie helpers ────────────────────────────────────────────────────

        private CookieOptions RefreshTokenCookieOptions() => new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/auth"
        };

        private void SetRefreshTokenCookie(string token)
            => Response.Cookies.Append("refreshToken", token, RefreshTokenCookieOptions());

        private void DeleteRefreshTokenCookie()
        {
            var opts = RefreshTokenCookieOptions();
            opts.Expires = DateTimeOffset.UtcNow.AddDays(-1);
            Response.Cookies.Append("refreshToken", "", opts);
        }

        // ── Context helpers ───────────────────────────────────────────────────

        private string ClientIp()
        {
            var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return string.IsNullOrEmpty(forwarded)
                ? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                : forwarded.Split(',')[0].Trim();
        }

        private string UserAgent() => Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

        // ── Endpoints ─────────────────────────────────────────────────────────

        [HttpPost("google/login")]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleCodeRequest request)
        {
            var traceId = HttpContext.TraceIdentifier;
            var ip = ClientIp();
            var ua = UserAgent();

            _logger.LogInformation(
                "[AUTH] google-login-start | TraceId={TraceId} | IP={IP} | UA={UA} | HasCode={HasCode} | HasVerifier={HasVerifier}",
                traceId, ip, ua,
                !string.IsNullOrWhiteSpace(request?.Code),
                !string.IsNullOrWhiteSpace(request?.CodeVerifier));

            try
            {
                if (string.IsNullOrWhiteSpace(request?.Code))
                {
                    _logger.LogWarning(
                        "[AUTH] google-login-rejected | TraceId={TraceId} | IP={IP} | Reason=MissingCode",
                        traceId, ip);
                    return BadRequest(new AuthResponse { Success = false, Message = "Authorization code is required", Error = "Invalid request" });
                }

                var result = await _authService.GoogleLoginAsync(request.Code, request.CodeVerifier);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "[AUTH] google-login-failed | TraceId={TraceId} | IP={IP} | UA={UA} | Error={Error}",
                        traceId, ip, ua, result.Error);
                    return Unauthorized(result);
                }

                if (result.RefreshTokenPlaintext != null)
                    SetRefreshTokenCookie(result.RefreshTokenPlaintext);

                _logger.LogInformation(
                    "[AUTH] google-login-success | TraceId={TraceId} | IP={IP} | UA={UA} | UserId={UserId} | Email={Email} | CookieSet=true",
                    traceId, ip, ua, result.User?.Id, result.User?.Email);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AUTH] google-login-exception | TraceId={TraceId} | IP={IP}",
                    traceId, ip);
                return StatusCode(500, new AuthResponse { Success = false, Message = "An error occurred during Google login", Error = "Internal server error" });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromForm] string username, [FromForm] string password)
        {
            var traceId = HttpContext.TraceIdentifier;
            var ip = ClientIp();
            var ua = UserAgent();

            _logger.LogInformation(
                "[AUTH] local-login-start | TraceId={TraceId} | IP={IP} | UA={UA} | Username={Username}",
                traceId, ip, ua, username);

            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning(
                        "[AUTH] local-login-rejected | TraceId={TraceId} | IP={IP} | Reason=MissingCredentials",
                        traceId, ip);
                    return BadRequest(new AuthResponse { Success = false, Message = "Username and password are required", Error = "Invalid request" });
                }

                var result = await _authService.LocalLoginAsync(username, password);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "[AUTH] local-login-failed | TraceId={TraceId} | IP={IP} | UA={UA} | Username={Username} | Error={Error}",
                        traceId, ip, ua, username, result.Error);
                    return Unauthorized(result);
                }

                if (result.RefreshTokenPlaintext != null)
                    SetRefreshTokenCookie(result.RefreshTokenPlaintext);

                _logger.LogInformation(
                    "[AUTH] local-login-success | TraceId={TraceId} | IP={IP} | UA={UA} | UserId={UserId} | Username={Username} | CookieSet=true",
                    traceId, ip, ua, result.User?.Id, username);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AUTH] local-login-exception | TraceId={TraceId} | IP={IP} | Username={Username}",
                    traceId, ip, username);
                return StatusCode(500, new AuthResponse { Success = false, Message = "An error occurred during login", Error = "Internal server error" });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh()
        {
            var traceId = HttpContext.TraceIdentifier;
            var ip = ClientIp();
            var ua = UserAgent();
            var hasCookie = !string.IsNullOrEmpty(Request.Cookies["refreshToken"]);

            _logger.LogInformation(
                "[AUTH] refresh-start | TraceId={TraceId} | IP={IP} | UA={UA} | HasCookie={HasCookie}",
                traceId, ip, ua, hasCookie);

            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning(
                    "[AUTH] refresh-rejected | TraceId={TraceId} | IP={IP} | UA={UA} | Reason=MissingCookie",
                    traceId, ip, ua);
                return Unauthorized(new AuthResponse { Success = false, Message = "No refresh token", Error = "Missing cookie" });
            }

            var result = await _authService.RefreshTokenAsync(refreshToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "[AUTH] refresh-failed | TraceId={TraceId} | IP={IP} | UA={UA} | Error={Error} | Message={Message}",
                    traceId, ip, ua, result.Error, result.Message);
                DeleteRefreshTokenCookie();
                return Unauthorized(result);
            }

            if (result.RefreshTokenPlaintext != null)
                SetRefreshTokenCookie(result.RefreshTokenPlaintext);

            _logger.LogInformation(
                "[AUTH] refresh-success | TraceId={TraceId} | IP={IP} | UA={UA} | UserId={UserId} | Email={Email} | NewCookieSet=true",
                traceId, ip, ua, result.User?.Id, result.User?.Email);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var traceId = HttpContext.TraceIdentifier;
            var ip = ClientIp();
            var ua = UserAgent();
            var hasCookie = !string.IsNullOrEmpty(Request.Cookies["refreshToken"]);

            _logger.LogInformation(
                "[AUTH] logout-start | TraceId={TraceId} | IP={IP} | UA={UA} | HasCookie={HasCookie}",
                traceId, ip, ua, hasCookie);

            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
                await _authService.RevokeRefreshTokenAsync(refreshToken);

            DeleteRefreshTokenCookie();

            _logger.LogInformation(
                "[AUTH] logout-success | TraceId={TraceId} | IP={IP} | UA={UA} | TokenRevoked={TokenRevoked}",
                traceId, ip, ua, hasCookie);

            return Ok();
        }
    }
}
