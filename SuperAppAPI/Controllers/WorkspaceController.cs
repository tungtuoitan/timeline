using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing workspace operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly ILogger<WorkspaceController> _logger;

        public WorkspaceController(IWorkspaceService workspaceService, ILogger<WorkspaceController> logger)
        {
            _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
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
        /// Gets all workspaces for the current user with optional filtering
        /// </summary>
        /// <param name="statusCode">Optional comma-separated status codes filter (e.g., "active,inactive")</param>
        /// <param name="deletedAt">Optional deleted status filter ("null" for active only, "notNull" for deleted only)</param>
        /// <param name="createdAtFrom">Optional created date from filter (ISO date string)</param>
        /// <param name="createdAtTo">Optional created date to filter (ISO date string)</param>
        /// <response code="200">Workspaces retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<WsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUserWorkspaces(
            [FromQuery] string? statusCode = null,
            [FromQuery] string? deletedAt = null,
            [FromQuery] string? createdAtFrom = null,
            [FromQuery] string? createdAtTo = null)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            // Build filter options
            var filterOptions = new FilterOptions
            {
                StatusCodes = !string.IsNullOrEmpty(statusCode)
                    ? statusCode.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : null,
                DeletedAt = deletedAt,
                CreatedFrom = !string.IsNullOrEmpty(createdAtFrom) && DateTime.TryParse(createdAtFrom, out var parsedFrom)
                    ? parsedFrom
                    : null,
                CreatedTo = !string.IsNullOrEmpty(createdAtTo) && DateTime.TryParse(createdAtTo, out var parsedTo)
                    ? parsedTo
                    : null
            };

            _logger.LogInformation("Retrieving all workspaces for userId: {UserId}, StatusCodes: {StatusCodes}, DeletedAt: {DeletedAt}, CreatedFrom: {CreatedFrom}, CreatedTo: {CreatedTo}",
                userId.Value,
                filterOptions.StatusCodes != null ? string.Join(",", filterOptions.StatusCodes) : "null",
                filterOptions.DeletedAt,
                filterOptions.CreatedFrom,
                filterOptions.CreatedTo);

            var response = await _workspaceService.GetAllUserWorkspacesAsync(userId.Value, filterOptions);

            _logger.LogInformation("Successfully retrieved {Count} workspaces for userId: {UserId}",
                response.Count, userId.Value);

            return Ok(response);
        }

        /// <summary>
        /// Gets workspace information with its complete hierarchical tree (tags, notes, and files)
        /// </summary>
        /// <response code="200">Workspace tree retrieved successfully</response>
        /// <response code="400">Invalid workspace ID</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="403">Access denied - no access to workspace</response>
        /// <response code="404">Workspace not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{workspaceId}/tree")]
        [ProducesResponseType(typeof(WorkspaceWithTreeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceTree(int workspaceId)
        {
            if (workspaceId <= 0)
            {
                _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
                throw new BadRequestException("Workspace ID must be a positive integer");
            }

            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }
            
            _logger.LogInformation("Retrieving workspace tree for workspaceId: {WorkspaceId}, userId: {UserId}", 
                workspaceId, userId.Value);

            var response = await _workspaceService.GetWorkspaceTreeAsync(workspaceId, userId.Value);

            _logger.LogInformation("Successfully retrieved workspace tree with {RootCount} root items for workspaceId: {WorkspaceId}",
                response?.Items?.Count ?? 0, workspaceId);

            return Ok(response);
        }

        /// <summary>
        /// Gets workspace information with its complete hierarchical tree (V2 - with full entity data)
        /// V2 structure: Clear separation between workspace_items properties and entity data
        /// Returns flat list with full entity data in 'Data' property
        /// </summary>
        /// <response code="200">Workspace tree retrieved successfully</response>
        /// <response code="400">Invalid workspace ID</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="403">Access denied - no access to workspace</response>
        /// <response code="404">Workspace not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{workspaceId}/tree/v2")]
        [ProducesResponseType(typeof(WorkspaceWithTreeResponseV2), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceTreeV2(int workspaceId)
        {
            if (workspaceId <= 0)
            {
                _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
                throw new BadRequestException("Workspace ID must be a positive integer");
            }

            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            _logger.LogInformation("Retrieving workspace tree V2 for workspaceId: {WorkspaceId}, userId: {UserId}",
                workspaceId, userId.Value);

            var response = await _workspaceService.GetWorkspaceTreeV2Async(workspaceId, userId.Value);

            _logger.LogInformation("Successfully retrieved workspace tree V2 with {ItemCount} items for workspaceId: {WorkspaceId}",
                response?.Items?.Count ?? 0, workspaceId);

            return Ok(response);
        }

        /// <summary>
        /// Creates a new folder or updates an existing folder in a workspace
        /// </summary>
        /// <response code="200">Folder created/updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Workspace or folder not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{workspaceId}/folders")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertFolder(int workspaceId, [FromBody] UpsertFolderRequest request)
        {
            if (workspaceId <= 0)
            {
                _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
                return BadRequest(new ResultOptions
                {
                    Success = false,
                    Message = "Workspace ID must be a positive integer",
                    Status = 400
                });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for upsert folder request");
                return BadRequest(new ResultOptions
                {
                    Success = false,
                    Message = "Invalid request data",
                    Object = ModelState,
                    Status = 400
                });
            }

            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var action = request.Id.HasValue ? "Updating" : "Creating";
            _logger.LogInformation("{Action} folder '{Name}' in workspace {WorkspaceId} for user {UserId}",
                action, request.Name, workspaceId, userId.Value);

            var result = await _workspaceService.UpsertFolderAsync(workspaceId, userId.Value, request);

            // Return appropriate status code based on result
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(result.Status ?? 500, result);
            }
        }

        /// <summary>
        /// Moves multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <response code="200">Items moved successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Workspace or items not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPatch("{workspaceId}/items/move")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MoveItems(int workspaceId, [FromBody] MoveItemsRequest request)
        {
            if (workspaceId <= 0)
            {
                _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
                return BadRequest(new ResultOptions
                {
                    Success = false,
                    Message = "Workspace ID must be a positive integer",
                    Status = 400
                });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for move items request");
                return BadRequest(new ResultOptions
                {
                    Success = false,
                    Message = "Invalid request data",
                    Object = ModelState,
                    Status = 400
                });
            }

            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            _logger.LogInformation("Moving {Count} items in workspace {WorkspaceId} for user {UserId}",
                request.Items.Count, workspaceId, userId.Value);

            var result = await _workspaceService.MoveItemsAsync(workspaceId, userId.Value, request);

            // Return appropriate status code based on result
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(result.Status ?? 500, result);
            }
        }

        /// <summary>
        /// Deletes multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        /// <response code="200">Items deleted successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Workspace or items not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{workspaceId}/items")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteItems(int workspaceId, [FromBody] DeleteItemsRequest request)
        {
            if (workspaceId <= 0)
            {
                _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
                return BadRequest(new ResultOptions
                {
                    Success = false,
                    Message = "Workspace ID must be a positive integer",
                    Status = 400
                });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for delete items request");
                return BadRequest(new ResultOptions
                {
                    Success = false,
                    Message = "Invalid request data",
                    Object = ModelState,
                    Status = 400
                });
            }

            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            _logger.LogInformation("Deleting {Count} items in workspace {WorkspaceId} for user {UserId}",
                request.Items.Count, workspaceId, userId.Value);

            var result = await _workspaceService.DeleteItemsAsync(workspaceId, userId.Value, request);

            // Return appropriate status code based on result
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(result.Status ?? 500, result);
            }
        }

        /// <summary>
        /// Adds an item (folder/note/file) to a workspace
        /// </summary>
        /// <response code="201">Item added successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Workspace or item not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{workspaceId}/items")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddItemToWorkspace(int workspaceId, [FromBody] AddItemToWorkspaceRequest request)
        {
            if (workspaceId <= 0)
            {
                _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
                return BadRequest(new ResultOptions
                {
                    Success = false,
                    Message = "Workspace ID must be a positive integer",
                    Status = 400
                });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for add item request");
                return BadRequest(new ResultOptions
                {
                    Success = false,
                    Message = "Invalid request data",
                    Object = ModelState,
                    Status = 400
                });
            }

            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            _logger.LogInformation("Adding {ChildType} (ID: {ChildId}) to workspace {WorkspaceId} for user {UserId}",
                request.ChildType, request.ChildId, workspaceId, userId.Value);

            var result = await _workspaceService.AddItemToWorkspaceAsync(workspaceId, userId.Value, request);

            // Return appropriate status code based on result
            if (result.Success)
            {
                return StatusCode(result.Status ?? 201, result);
            }
            else
            {
                return StatusCode(result.Status ?? 500, result);
            }
        }

        /// <summary>
        /// Batch upsert multiple workspace items (create or update) in a single request
        /// Use this for single item operations by passing an array with 1 element
        /// Pattern: 100% follows NotesController.UpsertNotes
        /// </summary>
        /// <param name="workspaceId">Workspace ID from route</param>
        /// <param name="requests">List of workspace item upsert data</param>
        /// <returns>Batch operation results</returns>
        /// <response code="200">Workspace items upserted successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{workspaceId}/items/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertWorkspaceItems(
            int workspaceId,
            [FromBody] List<UpsertWorkspaceItemRequest> requests)
        {
            // 1. Validate ModelState (giống NotesController)
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for batch upsert workspace items request");
                return BadRequest(ModelState);
            }

            // 2. Validate non-empty list (giống NotesController)
            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("Empty batch upsert request");
                return BadRequest("At least one workspace item is required");
            }

            // 3. Get userId from JWT token claims (giống NotesController)
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var userEmail = User.GetUserEmail();

            // 4. Set workspaceId, userId, and CreatedBy for all requests (giống NotesController)
            foreach (var request in requests)
            {
                request.WorkspaceId = workspaceId;
                request.UserId = userId.Value;
                request.CreatedBy = userEmail;
            }

            _logger.LogInformation(
                "Batch upserting {Count} workspace items for workspace: {WorkspaceId}, user: {UserId}",
                requests.Count, workspaceId, userId.Value);

            // 5. Call service layer (giống NotesController)
            var response = await _workspaceService.UpsertWorkspaceItemsAsync(requests, userId.Value);

            _logger.LogInformation(
                "Batch upsert workspace items completed for workspace: {WorkspaceId}, user: {UserId}, Success: {Success}",
                workspaceId, userId.Value, response.Success);

            return Ok(response);
        }
    }
}
