using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;
using SuperAppModels.DTOs.Requests;

namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileRepository _repository;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(
            IUserProfileRepository repository,
            ILogger<UserProfileController> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserProfile([FromQuery] string? appC = null)
        {
            try
            {
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

                _logger.LogInformation("Getting user profile for email: {Email}, AppC: {AppC}", userEmail, appC);

                var userProfile = await _repository.GetByEmailAsync(userEmail);

                if (userProfile == null)
                {
                    _logger.LogWarning("User profile not found for email: {Email}", userEmail);
                    return NotFound(new { Message = "User profile not found" });
                }

                _logger.LogInformation("Successfully retrieved user profile for email: {Email}", userEmail);
                return Ok(userProfile);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get user profile");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for user profile");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving user profile");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while retrieving the user profile" });
            }
        }

        [HttpGet("admin")]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserProfileByEmail([FromQuery] string email, [FromQuery] string? appC = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Empty email provided for admin user profile lookup");
                    return BadRequest(new { Message = "Email is required" });
                }

                var currentUserEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

                _logger.LogInformation("Admin access attempt: User {CurrentUser} requesting profile for {TargetEmail}",
                    currentUserEmail, email);

                _logger.LogInformation("Getting user profile for email: {Email}, AppC: {AppC}", email, appC);

                var userProfile = await _repository.GetByEmailAsync(email);

                if (userProfile == null)
                {
                    _logger.LogWarning("User profile not found for email: {Email}", email);
                    return NotFound(new { Message = "User profile not found" });
                }

                _logger.LogInformation("Successfully retrieved user profile for email: {Email}", email);
                return Ok(userProfile);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for admin get user profile");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for admin user profile lookup");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving user profile for {Email}", email);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while retrieving the user profile" });
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for update user profile request");
                    return BadRequest(ModelState);
                }

                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                var targetEmail = string.IsNullOrEmpty(request.Email) ? userEmail : request.Email;

                _logger.LogInformation("Updating user profile for email: {Email}, AppC: {AppC}",
                    targetEmail, request.AppC);

                var userProfile = new UserProfile
                {
                    Email = targetEmail,
                    AppC = request.AppC
                };

                var result = await _repository.CreateOrUpdateAsync(userProfile);

                _logger.LogInformation("Successfully updated user profile for email: {Email}", targetEmail);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for user profile update");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for user profile update");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating user profile");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while updating the user profile" });
            }
        }
    }
}
