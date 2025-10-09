using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using MediatR;
using SuperApp.Application.Features.Authentication.Commands.Login;
using SuperApp.Application.Features.Authentication.Commands.Signup;
using SuperApp.Application.Features.Authentication.Commands.GoogleLogin;
using FluentValidation;
using ValidationException = FluentValidation.ValidationException;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Handles authentication operations including signup, login, and token management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IMediator _mediator;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IConfiguration config,
            IMediator mediator,
            HttpClient httpClient,
            ILogger<AuthController> logger)
        {
            _config = config;
            _mediator = mediator;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new user account with email and password
        /// </summary>
        /// <param name="request">User signup information including email, password, and personal details</param>
        /// <returns>Authentication response with user data and JWT token</returns>
        /// <response code="200">User created successfully with authentication token</response>
        /// <response code="400">Invalid request data or user already exists</response>
        /// <response code="500">Internal server error</response>
        [AllowAnonymous]
        [HttpPost("signup")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponse>> Signup([FromBody] SignupRequest request)
        {
            _logger.LogInformation("New signup request for email: {Email}", request.Email);

            try
            {
                var command = new SignupCommand(request);
                var result = await _mediator.Send(command);

                _logger.LogInformation("User successfully created with ID: {UserId}", result.User?.Id);
                return Ok(result);
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                _logger.LogWarning("Validation failed during signup for email: {Email}, Error: {ErrorMessage}", request.Email, ex.Message);
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Validation failed",
                    User = null
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Signup failed for email: {Email}, Reason: {Message}", request.Email, ex.Message);
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = ex.Message,
                    User = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during signup for email: {Email}", request.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred during signup",
                    User = null
                });
            }
        }

        /// <summary>
        /// Authenticates a user with email/phone and password
        /// </summary>
        /// <param name="request">Login credentials including email/phone and password</param>
        /// <returns>Authentication response with user data and JWT token</returns>
        /// <response code="200">Login successful with authentication token</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="500">Internal server error</response>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt for: {Identifier}", request.Email ?? request.Phone);

            try
            {
                var command = new LoginCommand(request);
                var result = await _mediator.Send(command);

                _logger.LogInformation("User successfully logged in with ID: {UserId}", result.User?.Id);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed for: {Identifier}, Reason: {Message}", 
                    request.Email ?? request.Phone, ex.Message);
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = ex.Message,
                    User = null
                });
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                _logger.LogWarning("Validation failed during login for: {Identifier}, Error: {ErrorMessage}", 
                    request.Email ?? request.Phone, ex.Message);
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid request data",
                    User = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for: {Identifier}", request.Email ?? request.Phone);
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred during login",
                    User = null
                });
            }
        }

        /// <summary>
        /// Authenticates a user with Google OAuth credentials
        /// </summary>
        /// <param name="request">Google authentication data including email and user information</param>
        /// <returns>Authentication response with user data and JWT token</returns>
        /// <response code="200">Google login successful with authentication token</response>
        /// <response code="400">Invalid Google credentials or user data</response>
        /// <response code="500">Internal server error</response>
        [AllowAnonymous]
        [HttpPost("login/google")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            _logger.LogInformation("Google login for email: {Email}", request.Email);

            try
            {
                var command = new GoogleLoginCommand(request);
                var result = await _mediator.Send(command);

                _logger.LogInformation("Google user successfully authenticated with ID: {UserId}", result.User?.Id);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Google login failed for email: {Email}, Reason: {Message}", 
                    request.Email, ex.Message);
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = ex.Message,
                    User = null
                });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validation failed during Google login for email: {Email}, Errors: {Errors}",
                    request.Email, string.Join(", ", ex.Errors.Select(f => f.ErrorMessage)));
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid request data",
                    User = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Google login for email: {Email}", request.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred during Google authentication",
                    User = null
                });
            }
        }

        /// <summary>
        /// Exchanges Google authorization code for access and refresh tokens
        /// </summary>
        /// <param name="request">Google authorization code from OAuth flow</param>
        /// <returns>Google OAuth tokens including access_token and refresh_token</returns>
        /// <response code="200">Token exchange successful</response>
        /// <response code="400">Invalid authorization code</response>
        /// <response code="500">Internal server error during token exchange</response>
        [AllowAnonymous]
        [HttpPost("google/token")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExchangeGoogleToken([FromBody] GoogleCodeRequest request)
        {
            _logger.LogInformation("Exchanging Google authorization code");

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                _logger.LogWarning("Google token exchange failed: Authorization code is required");
                return BadRequest(new { error = "Authorization code is required" });
            }

            try
            {
                var clientId = _config["OAuth:ClientId"];
                var clientSecret = _config["OAuth:ClientSecret"];
                var redirectUri = _config["OAuth:RedirectUri"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("OAuth configuration missing: ClientId or ClientSecret not configured");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "OAuth configuration error" });
                }

                var tokenUrl = "https://oauth2.googleapis.com/token";
                var formData = new Dictionary<string, string>
                {
                    { "code", request.Code },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "redirect_uri", redirectUri ?? "" },
                    { "grant_type", "authorization_code" }
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(tokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Google token exchange failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                    return BadRequest(new { error = "Invalid authorization code or OAuth configuration" });
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokens = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                _logger.LogInformation("Google token exchange successful");
                return Ok(tokens);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during Google token exchange");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "Failed to communicate with Google OAuth server" });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Google token response");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "Invalid response from Google OAuth server" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Google token exchange");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "An unexpected error occurred during token exchange" });
            }
        }

        /// <summary>
        /// Retrieves current authenticated user information from JWT token
        /// </summary>
        /// <returns>Current user profile information and authentication status</returns>
        /// <response code="200">User information retrieved successfully</response>
        /// <response code="401">User is not authenticated or token is invalid</response>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public ActionResult GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;
                
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                           ?? User.FindFirst("email")?.Value;

                var firstName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
                              ?? User.FindFirst("given_name")?.Value;

                var lastName = User.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value
                             ?? User.FindFirst("family_name")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Invalid token: UserId claim not found");
                    return Unauthorized(new { message = "Invalid token or user not found" });
                }

                var userInfo = new
                {
                    userId = userId,
                    email = email,
                    firstName = firstName,
                    lastName = lastName,
                    isAuthenticated = User.Identity?.IsAuthenticated ?? false
                };

                _logger.LogInformation("User information retrieved for ID: {UserId}", userId);
                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user information");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An error occurred while retrieving user information" });
            }
        }
    }
}
