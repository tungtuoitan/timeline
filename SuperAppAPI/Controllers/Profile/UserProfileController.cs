using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.Models;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.Profile
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserProfileController : BaseAuthController
    {
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<UserProfileController> _logger;
        private readonly IMapper _mapper;

        public UserProfileController(
            IUserProfileService userProfileService,
            ILogger<UserProfileController> logger,
            IMapper mapper)
        {
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserProfile([FromQuery] string? appC = null)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User ID not found in token");
            }

            _logger.LogInformation("Getting user profile for userId: {UserId}, AppC: {AppC}", userId.Value, appC);

            var result = await _userProfileService.GetUserProfileByUserIdAsync(userId.Value);

            _logger.LogInformation("Retrieved user profile for userId: {UserId}, Success: {Success}",
                userId.Value, result.Success);

            return Ok(result);
        }

        /// <summary>
        /// Upsert user profile (insert if not exists, update if exists)
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertUserProfile([FromBody] UpdateUserProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for upsert user profile request");
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("User ID not found or invalid in claims");
                return Unauthorized(new { Message = "User ID not found or invalid in authentication token" });
            }

            _logger.LogInformation("Upserting user profile for userId: {UserId}", userId.Value);

            // Map request to UserProfile entity
            var userProfile = _mapper.Map<UserProfile>(request);
            userProfile.UserId = userId.Value;

            var result = await _userProfileService.UpsertUserProfileAsync(userProfile);

            _logger.LogInformation("Upserted user profile for userId: {UserId}, Success: {Success}",
                userId.Value, result.Success);

            return Ok(result);
        }
    }
}
