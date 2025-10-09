using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using UserProfileDataRepositories.Ins;
using SuperAppModels.Models;
using SuperAppModels.DTOs.Requests;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing user profile operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Restore authorization for all endpoints - security critical!
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

        /// <summary>
        /// Gets the current authenticated user's profile
        /// </summary>
        /// <returns>User profile data as JSON</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Profile not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserProfile([FromQuery] string? appC = null)
        {
            try
            {
                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Getting user profile for email: {Email}, AppC: {AppC}", userEmail, appC);

                var userProfile = await _repository.GetUserProfileByEmailAsync(userEmail);

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

        /// <summary>
        /// Gets a user profile by email (admin only)
        /// </summary>
        /// <param name="email">User email to retrieve profile for</param>
        /// <param name="appC">Application code</param>
        /// <returns>User profile data</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="400">Invalid email provided</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="403">Forbidden - admin access required</response>
        /// <response code="404">Profile not found</response>
        /// <response code="500">Internal server error</response>
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

                var currentUserEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                // TODO: Add role-based authorization check for admin users
                // For now, log the admin access attempt
                _logger.LogInformation("Admin access attempt: User {CurrentUser} requesting profile for {TargetEmail}", 
                    currentUserEmail, email);

                _logger.LogInformation("Getting user profile for email: {Email}, AppC: {AppC}", email, appC);

                var userProfile = await _repository.GetUserProfileByEmailAsync(email);

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

        /// <summary>
        /// Updates the current authenticated user's profile
        /// </summary>
        /// <param name="request">Profile update data</param>
        /// <returns>Update operation result</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPut]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
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

                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                // Ensure the user can only update their own profile (unless admin)
                if (!string.IsNullOrEmpty(request.Email) && request.Email != userEmail)
                {
                    // TODO: Add role-based check to allow admin users to update other profiles
                    _logger.LogWarning("User {CurrentUser} attempted to update profile for different user {TargetUser}", 
                        userEmail, request.Email);
                    return Forbid("You can only update your own profile");
                }

                // Use the authenticated user's email
                var targetEmail = string.IsNullOrEmpty(request.Email) ? userEmail : request.Email;

                _logger.LogInformation("Updating user profile for email: {Email}, AppC: {AppC}", 
                    targetEmail, request.AppC);

                var result = await _repository.IuUserProfile(
                    targetEmail,
                    request.AppC ?? string.Empty,
                    request.UserProfileJson ?? string.Empty);

                if (result?.Success == true)
                {
                    _logger.LogInformation("Successfully updated user profile for email: {Email}", targetEmail);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Failed to update user profile for email: {Email}. Result: {Result}", 
                        targetEmail, result?.ErrorMessage ?? "Unknown error");
                    return BadRequest(result ?? new ResultOptions 
                    { 
                        Success = false, 
                        ErrorMessage = "Failed to update user profile" 
                    });
                }
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
