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
        /// <summary>
        /// Get all target entities that have linked this keyword (reverse lookup)
        /// </summary>
        [HttpGet("keyword-targets")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetKeywordTargets([FromQuery] int keywordId)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var response = await _keywordService.GetKeywordTargetsAsync(keywordId);
            return Ok(response);
        }

        /// <summary>
        /// Get all keywords linked to a target entity
        /// </summary>
        [HttpGet("target-keywords")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTargetKeywords([FromQuery] int targetId, [FromQuery] string targetType)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            _logger.LogInformation("Getting keywords for targetId: {TargetId}, targetType: {TargetType}", targetId, targetType);

            var response = await _keywordService.GetTargetKeywordsAsync(targetId, targetType);
            return Ok(response);
        }

        /// <summary>
        /// Link a keyword to a target entity
        /// </summary>
        [HttpPost("target-keywords")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LinkTargetKeyword([FromBody] LinkTargetKeywordRequest request)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            if (request == null)
                return BadRequest("Request body is required");

            _logger.LogInformation("Linking keywordId: {KeywordId} to targetId: {TargetId} ({TargetType})",
                request.KeywordId, request.TargetId, request.TargetType);

            var response = await _keywordService.LinkTargetKeywordAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Unlink a keyword from a target entity
        /// </summary>
        [HttpDelete("target-keywords/{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UnlinkTargetKeyword(int id)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            _logger.LogInformation("Unlinking TargetKeyword with ID: {Id}", id);

            var response = await _keywordService.UnlinkTargetKeywordAsync(id);
            return Ok(response);
        }

        /// <summary>
        /// Full keyword sync: creates missing keywords, updates name/link mismatches.
        /// Returns a report with counts and detail lists.
        /// </summary>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(KeywordSyncReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncKeywords()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            _logger.LogInformation("Starting keyword sync for userId: {UserId}", userId.Value);

            var report = await _keywordService.SyncKeywordsAsync(userId.Value);

            _logger.LogInformation(
                "Keyword sync done — userId: {UserId}, total: {Total}, created: {Created}, updated: {Updated}",
                userId.Value, report.TotalKeywords, report.CreatedCount, report.UpdatedCount);

            return Ok(report);
        }
    }
}
