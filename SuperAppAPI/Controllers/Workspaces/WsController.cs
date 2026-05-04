using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.Workspaces
{
    /// <summary>
    /// Controller for managing workspace list operations (ws.workspaces)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WsController : ControllerBase
    {
        private readonly IWsService _wsService;
        private readonly ILogger<WsController> _logger;

        public WsController(IWsService wsService, ILogger<WsController> logger)
        {
            _wsService = wsService;
            _logger = logger;
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
        /// Get authenticated user email from JWT claims
        /// </summary>
        private string? GetAuthenticatedUserEmail()
        {
            return User.GetUserEmail();
        }

        /// <summary>
        /// Gets all workspaces with optional filtering for the authenticated user
        /// </summary>
        /// <param name="searchText">Optional search text filter</param>
        /// <param name="tagIds">Optional tag IDs filter</param>
        /// <param name="statusCode">Optional comma-separated status codes filter (e.g., "active,inactive")</param>
        /// <param name="deletedAt">Optional deleted status filter ("null" for active only, "notNull" for deleted only)</param>
        /// <param name="createdAtFrom">Optional created date from filter (ISO date string)</param>
        /// <param name="createdAtTo">Optional created date to filter (ISO date string)</param>
        /// <param name="ids">Optional comma-separated workspace IDs (e.g., "1,2,3") for restoring tabs</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <returns>ResultOptions containing list of workspaces matching the criteria</returns>
        /// <response code="200">Workspaces retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetWorkspaces(
            [FromQuery] string? searchText = null,
            [FromQuery] List<int>? tagIds = null,
            [FromQuery] string? statusCode = null,
            [FromQuery] string? deletedAt = null,
            [FromQuery] string? createdAtFrom = null,
            [FromQuery] string? createdAtTo = null,
            [FromQuery] string? ids = null,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null)
        {
            // Get userId from JWT token claims
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var userEmail = GetAuthenticatedUserEmail();

            // Build filter options
            var filterOptions = new WsFilterOptions
            {
                UserId = userId.Value,
                SearchText = searchText,
                TagIds = tagIds,
                StatusCodes = !string.IsNullOrEmpty(statusCode)
                    ? statusCode.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : null,
                DeletedAt = deletedAt,
                CreatedFrom = !string.IsNullOrEmpty(createdAtFrom) && DateTime.TryParse(createdAtFrom, out var parsedFrom)
                    ? parsedFrom
                    : null,
                CreatedTo = !string.IsNullOrEmpty(createdAtTo) && DateTime.TryParse(createdAtTo, out var parsedTo)
                    ? parsedTo
                    : null,
                Ids = !string.IsNullOrEmpty(ids)
                    ? ids.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
                    : null,
                PageNumber = page,
                PageSize = pageSize
            };

            _logger.LogInformation(
                "Retrieving workspaces for userId: {UserId}, UserEmail: {UserEmail}, SearchText: {SearchText}, TagIds: {TagIds}, StatusCodes: {StatusCodes}, DeletedAt: {DeletedAt}, CreatedFrom: {CreatedFrom}, CreatedTo: {CreatedTo}, Page: {Page}, PageSize: {PageSize}",
                userId.Value, userEmail, filterOptions.SearchText,
                filterOptions.TagIds != null ? string.Join(",", filterOptions.TagIds) : "null",
                filterOptions.StatusCodes != null ? string.Join(",", filterOptions.StatusCodes) : "null",
                filterOptions.DeletedAt, filterOptions.CreatedFrom, filterOptions.CreatedTo,
                filterOptions.PageNumber, filterOptions.PageSize);

            var response = await _wsService.GetWorkspacesAsync(filterOptions);

            _logger.LogInformation("Successfully retrieved workspaces for user: {UserEmail}, Success: {Success}",
                userEmail, response.Success);

            return Ok(response);
        }

        /// <summary>
        /// Batch upsert multiple workspaces (create or update) in a single request
        /// Use this for single workspace operations by passing an array with 1 element
        /// </summary>
        /// <param name="requests">List of workspace upsert data</param>
        /// <returns>Batch operation results</returns>
        /// <response code="200">Workspaces upserted successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [HttpPost("batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertWorkspacesBatch([FromBody] List<UpsertWorkspaceRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for batch upsert workspaces request");
                return BadRequest(ModelState);
            }

            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("Empty batch upsert request");
                return BadRequest("At least one workspace is required");
            }

            // Get userId from JWT token claims
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var userEmail = GetAuthenticatedUserEmail();

            // Set userId for all requests
            foreach (var request in requests)
            {
                request.UserId = userId.Value;

                // Clean up TagIds - remove invalid values (0 or negative)
                if (request.TagIds != null && request.TagIds.Any())
                {
                    request.TagIds = request.TagIds.Where(tagId => tagId > 0).Distinct().ToList();
                    if (!request.TagIds.Any())
                    {
                        request.TagIds = null;
                    }
                }
            }

            _logger.LogInformation("Batch upserting {Count} workspaces for user: {UserEmail}",
                requests.Count, userEmail);

            var response = await _wsService.UpsertWorkspacesBatchAsync(requests);

            _logger.LogInformation("Batch upsert workspaces completed for user: {UserEmail}, Success: {Success}",
                userEmail, response.Success);

            return Ok(response);
        }

        /// <summary>
        /// Gets a specific workspace by ID for the authenticated user
        /// </summary>
        /// <param name="id">Workspace ID</param>
        /// <returns>Workspace details if found and accessible</returns>
        /// <response code="200">Workspace retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Workspace not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceById(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", id);
                throw new BadRequestException("Workspace ID must be a positive integer");
            }

            var userEmail = GetAuthenticatedUserEmail();

            _logger.LogInformation("Retrieving workspace {WorkspaceId} for user: {UserEmail}", id, userEmail);

            var response = await _wsService.GetWorkspaceByIdAsync(id);

            _logger.LogInformation("Retrieved workspace {WorkspaceId} for user: {UserEmail}, Success: {Success}", id, userEmail, response.Success);
            return Ok(response);
        }

      
        /// <summary>
        /// Hard deletes one or more workspaces with CASCADE to all items (folders/notes/files) for the authenticated user
        /// For soft delete, use the Upsert endpoint with deletedAt timestamp
        /// </summary>
        /// <param name="id">Workspace ID to delete (supports comma-separated IDs, e.g., "1,2,3")</param>
        /// <returns>Success status with deleted count</returns>
        /// <response code="200">Workspace(s) deleted successfully with all items</response>
        /// <response code="400">Invalid workspace ID(s)</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Workspace(s) not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteWorkspace(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Empty workspace ID(s) provided for deletion");
                throw new BadRequestException("Workspace ID(s) must be provided");
            }

            var userEmail = GetAuthenticatedUserEmail();

            _logger.LogInformation("Hard deleting workspace(s) {WorkspaceIds} for user: {UserEmail} with CASCADE",
                id, userEmail);

            // Pass the string directly (no parsing needed - SP handles it)
            var response = await _wsService.DeleteWorkspacesAsync(id);

            _logger.LogInformation("Delete workspaces result for user: {UserEmail}, Success: {Success}", userEmail, response.Success);
            return Ok(response);
        }
    }
}
