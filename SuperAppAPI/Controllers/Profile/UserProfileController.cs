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
    public class UserProfileController : ControllerBase
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

        /// <summary>
        /// Get authenticated user ID from JWT claims
        /// </summary>
        private int? GetAuthenticatedUserId()
        {
            var userIdClaim = User.GetUserId();
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }
            return userId;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserProfile([FromQuery] string? appC = null)
        {
            var userId = GetAuthenticatedUserId();
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

        [HttpGet("admin")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserProfileByUserId([FromQuery] int targetUserId, [FromQuery] string? appC = null)
        {
            if (targetUserId <= 0)
            {
                _logger.LogWarning("Invalid userId provided for admin user profile lookup");
                return BadRequest(new { Message = "UserId must be greater than 0" });
            }

            var currentUserId = GetAuthenticatedUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized("User ID not found in token");
            }

            _logger.LogInformation("Admin access: User {CurrentUserId} requesting profile for {TargetUserId}, AppC: {AppC}",
                currentUserId.Value, targetUserId, appC);

            var result = await _userProfileService.GetUserProfileByUserIdAsync(targetUserId);

            _logger.LogInformation("Retrieved user profile for userId: {UserId}, Success: {Success}",
                targetUserId, result.Success);

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

            var userId = GetAuthenticatedUserId();
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
