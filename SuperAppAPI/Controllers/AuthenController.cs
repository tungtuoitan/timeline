using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Mos;
using UserProfileDataServices.Ins;

namespace SuperAppAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAuthService _authService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IConfiguration config,
            IAuthService authSe,
            HttpClient httpClient,
            ILogger<AuthController> logger)
        {
            _config = config;
            _authService = authSe;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Sign up a new user
        /// </summary>
        [AllowAnonymous]
        [HttpPost("signup")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<AuthResponse>> Signup([FromBody] SignupRequest request)
        {
            _logger.LogInformation("New signup request for email: {Email}", request.Email);

            var userModel = new UserModel
            {
                Email = request.Email,
                Phone = request.Phone,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Birthday = request.Birthday,
                Type = "signUpDefault"
            };

            var result = await _authService.IuUser(userModel);

            var response = new AuthResponse
            {
                Success = result.Success,
                Message = result.Message,
                User = result.Success ? new UserData
                {
                    Id = result.Data.Id,
                    Email = result.Data.Email,
                    Phone = result.Data.Phone,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    Token = result.Data.Token
                } : null
            };

            return Ok(response);
        }

        /// <summary>
        /// Login with email/phone and password
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt for: {Identifier}", request.Email ?? request.Phone);

            var userModel = new UserModel
            {
                Email = request.Email,
                Phone = request.Phone,
                Password = request.Password,
                Type = "loginByDefault"
            };

            var result = await _authService.IuUser(userModel);

            if (!result.Success)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = result.Message
                });
            }

            var response = new AuthResponse
            {
                Success = true,
                Message = result.Message,
                User = new UserData
                {
                    Id = result.Data.Id,
                    Email = result.Data.Email,
                    Phone = result.Data.Phone,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    Token = result.Data.Token
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Login with Google account
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login/google")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            _logger.LogInformation("Google login for email: {Email}", request.Email);

            var userModel = new UserModel
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Type = "loginByGoogle"
            };

            var result = await _authService.IuUser(userModel);

            var response = new AuthResponse
            {
                Success = result.Success,
                Message = result.Message,
                User = new UserData
                {
                    Id = result.Data.Id,
                    Email = result.Data.Email,
                    Phone = result.Data.Phone,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    Token = result.Data.Token
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Exchange Google authorization code for tokens
        /// </summary>
        [HttpPost("google/token")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ExchangeGoogleToken([FromBody] GoogleCodeRequest request)
        {
            _logger.LogInformation("Exchanging Google authorization code");

            var clientId = _config["OAuth:ClientId"];
            var clientSecret = _config["OAuth:ClientSecret"];
            var redirectUri = _config["OAuth:RedirectUri"];
            var tokenUrl = "https://oauth2.googleapis.com/token";

            var formData = new Dictionary<string, string>
            {
                { "code", request.Code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var content = new FormUrlEncodedContent(formData);

            try
            {
                var response = await _httpClient.PostAsync(tokenUrl, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokens = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                return Ok(tokens);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to exchange Google authorization code");
                return StatusCode(500, new { error = "Failed to exchange authorization code" });
            }
        }

        /// <summary>
        /// Get current user information from JWT token
        /// </summary>
        // [Authorize]  // Temporarily commented out
        [HttpGet("me")]
        [ProducesResponseType(200)]
        public ActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
            
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                       ?? User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token or user not found" });
            }

            return Ok(new
            {
                userId = userId,
                email = email,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                claims = User.Claims.Select(c => new { type = c.Type, value = c.Value })
            });
        }
    }

    public class GoogleCodeRequest
    {
        public string Code { get; set; } = string.Empty;
    }
}
