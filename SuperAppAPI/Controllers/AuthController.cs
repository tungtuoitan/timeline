using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Authentication Controller
    /// Handles user authentication via Google OAuth and local login
    /// </summary>
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

        private void SetRefreshTokenCookie(string token)
        {
            Response.Cookies.Append("refreshToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/api/auth"
            });
        }

        [HttpPost("google/login")]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleCodeRequest request)
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogInformation("Google login request received. TraceId={TraceId}, HasCode={HasCode}, HasVerifier={HasVerifier}",
                traceId, !string.IsNullOrWhiteSpace(request?.Code), !string.IsNullOrWhiteSpace(request?.CodeVerifier));

            try
            {
                if (string.IsNullOrWhiteSpace(request?.Code))
                {
                    _logger.LogWarning("Google login rejected: missing authorization code. TraceId={TraceId}", traceId);
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Authorization code is required",
                        Error = "Invalid request"
                    });
                }

                var result = await _authService.GoogleLoginAsync(request.Code, request.CodeVerifier);

                if (!result.Success)
                {
                    _logger.LogWarning("Google login failed. TraceId={TraceId}, Error={Error}", traceId, result.Error);
                    return Unauthorized(result);
                }

                if (result.RefreshTokenPlaintext != null)
                    SetRefreshTokenCookie(result.RefreshTokenPlaintext);

                _logger.LogInformation("Google login successful. TraceId={TraceId}, Email={Email}", traceId, result.User?.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during Google login. TraceId={TraceId}", traceId);
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during Google login",
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Local login with username and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromForm] string username, [FromForm] string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Username and password are required",
                        Error = "Invalid request"
                    });
                }

                var result = await _authService.LocalLoginAsync(username, password);

                if (!result.Success)
                {
                    _logger.LogWarning("Local login failed for user: {Username}", username);
                    return Unauthorized(result);
                }

                if (result.RefreshTokenPlaintext != null)
                    SetRefreshTokenCookie(result.RefreshTokenPlaintext);

                _logger.LogInformation("User {Username} logged in successfully", username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during local login");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during login",
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Refresh access token using HttpOnly cookie refresh token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new AuthResponse { Success = false, Message = "No refresh token", Error = "Missing cookie" });

            var result = await _authService.RefreshTokenAsync(refreshToken);
            if (!result.Success)
            {
                Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
                return Unauthorized(result);
            }

            if (result.RefreshTokenPlaintext != null)
                SetRefreshTokenCookie(result.RefreshTokenPlaintext);

            return Ok(result);
        }

        /// <summary>
        /// Logout - revoke refresh token and clear cookie
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
                await _authService.RevokeRefreshTokenAsync(refreshToken);

            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
            return Ok();
        }
    }
}
