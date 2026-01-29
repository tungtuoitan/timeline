using Microsoft.AspNetCore.Mvc;
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

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Google OAuth login
        /// Exchange authorization code for JWT token
        /// </summary>
        /// <param name="request">Google authorization code</param>
        /// <returns>Authentication response with JWT token</returns>
        [HttpPost("google/login")]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                {
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
                    _logger.LogWarning("Google login failed: {Error}", result.Error);
                    return Unauthorized(result);
                }

                _logger.LogInformation("User {Email} logged in successfully via Google", result.User?.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
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
        /// <param name="username">Username or email</param>
        /// <param name="password">Password</param>
        /// <returns>Authentication response with JWT token</returns>
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
    }
}
