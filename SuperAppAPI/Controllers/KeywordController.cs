using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Services;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for keyword operations
    /// Handles both internal keywords (workspaces/folders/notes/headings) and external keywords
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KeywordController : ControllerBase
    {
        private readonly KeywordServiceV2 _keywordService;
        private readonly ILogger<KeywordController> _logger;

        public KeywordController(
            KeywordServiceV2 keywordService,
            ILogger<KeywordController> logger)
        {
            _keywordService = keywordService ?? throw new ArgumentNullException(nameof(keywordService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        /// <summary>
        /// Gets all keywords for the current user (internal + external) with LongLink computed runtime
        /// </summary>
        /// <response code="200">Keywords retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<KeywordDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllKeywords()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            _logger.LogInformation("Retrieving all keywords for userId: {UserId}", userId.Value);

            var keywords = await _keywordService.GetKeywordsAsync(userId.Value);

            _logger.LogInformation("Successfully retrieved {Count} keywords for userId: {UserId}",
                keywords.Count, userId.Value);

            return Ok(keywords);
        }

        /// <summary>
        /// Batch upsert external keywords
        /// </summary>
        /// <param name="requests">List of external keyword upsert requests</param>
        /// <response code="200">External keywords upserted successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("external/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertExternalKeywords([FromBody] List<UpsertExternalKeywordRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for upsert external keywords request");
                return BadRequest(ModelState);
            }

            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("Empty batch request for external keywords");
                return BadRequest("At least one keyword is required");
            }

            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            // Set userId for all requests
            foreach (var request in requests)
            {
                request.UserId = userId.Value;
            }

            _logger.LogInformation("Upserting {Count} external keywords for userId: {UserId}",
                requests.Count, userId.Value);

            var result = await _keywordService.UpsertExternalKeywordsAsync(userId.Value, requests);

            _logger.LogInformation("Upsert external keywords completed for userId: {UserId}, Success: {Success}",
                userId.Value, result.Success);

            return Ok(result);
        }
    }
}
