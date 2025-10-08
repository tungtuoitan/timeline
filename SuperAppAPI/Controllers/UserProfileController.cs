using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.Mos;
using SuperAppModels.DTOs;
using UserProfileDataRepositories.Ins;

namespace SuperAppAPI.Controllers
{
    // [Authorize]  // Temporarily commented out - Require authentication for all endpoints
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileRepositoy _repository;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(
            IUserProfileRepositoy repository,
            ILogger<UserProfileController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Get user profile as JSON
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(UserProfile), 200)]
        public async Task<ActionResult<UserProfile>> GetUserProfile(
            [FromQuery] string email,
            [FromQuery] string appC)
        {
            _logger.LogInformation("Getting user profile for email: {Email}", email);

            var userProfile = await _repository.GetUserProfileJson(email, appC);

            return Ok(userProfile);
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(ResultOptions), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ResultOptions>> UpdateUserProfile(
            [FromBody] UpdateUserProfileRequest request)
        {
            _logger.LogInformation("Updating user profile for email: {Email}", request.Email);

            var result = await _repository.IuUserProfile(
                request.Email,
                request.AppC,
                request.UserProfileJson);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
    }

    public class UpdateUserProfileRequest
    {
        public string Email { get; set; } = string.Empty;
        public string AppC { get; set; } = string.Empty;
        public string UserProfileJson { get; set; } = string.Empty;
    }
}
