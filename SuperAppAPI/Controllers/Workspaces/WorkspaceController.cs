using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;
using SuperAppModels.Time;

namespace SuperAppAPI.Controllers.Workspaces
{
    /// <summary>
    /// Controller for managing workspace operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkspaceController : BaseAuthController
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly IWorkspaceItemService _workspaceItemService;
        private readonly ILogger<WorkspaceController> _logger;

        public WorkspaceController(
            IWorkspaceService workspaceService,
            IWorkspaceItemService workspaceItemService,
            ILogger<WorkspaceController> logger)
        {
            _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
            _workspaceItemService = workspaceItemService ?? throw new ArgumentNullException(nameof(workspaceItemService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var userId = GetUserId();
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
                CreatedFrom = TimeParsing.ParseInstantLenient(createdAtFrom),
                CreatedTo = TimeParsing.ParseInstantLenientEnd(createdAtTo)
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
        /// Gets workspace information with its complete hierarchical tree (V2 - with full entity data)
        /// V2 structure: Clear separation between workspace_items properties and entity data
        /// Returns flat list with full entity data in 'Data' property wrapped in ResultOption
        ///
        /// ⚠️ PERFORMANCE NOTE - FILTERING MOVED TO FRONTEND:
        /// This endpoint returns ALL workspace items without server-side filtering.
        /// The statusCode and deletedAt query parameters are kept for backward compatibility
        /// but are IGNORED by the backend. Frontend handles all filtering.
        ///
        /// Performance characteristics:
        /// - ✅ GOOD for workspaces with &lt; 3,000 items (filter time &lt; 150ms)
        /// - ⚠️ ACCEPTABLE for 3,000-5,000 items (150-300ms, needs debouncing)
        /// - ❌ POOR for &gt; 5,000 items (&gt; 300ms, consider backend filtering)
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <param name="statusCode">DEPRECATED - Kept for backward compatibility but ignored (filter in frontend)</param>
        /// <param name="deletedAt">DEPRECATED - Kept for backward compatibility but ignored (filter in frontend)</param>
        /// <response code="200">Workspace tree retrieved successfully</response>
        /// <response code="400">Invalid workspace ID</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="403">Access denied - no access to workspace</response>
        /// <response code="404">Workspace not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{workspaceId}/tree/v2")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceTreeV2(
            int workspaceId,
            [FromQuery] string? statusCode = null,
            [FromQuery] string? deletedAt = null)
        {
            if (workspaceId <= 0)
            {
                _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
                throw new BadRequestException("Workspace ID must be a positive integer");
            }

            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            // ⚠️ CHANGED: Parameters kept for backward compatibility but NOT used
            // Frontend handles all filtering (statusCode, deletedAt, search)
            _logger.LogInformation("Retrieving workspace tree V2 for workspaceId: {WorkspaceId}, userId: {UserId} (NO SERVER FILTERING - params ignored)",
                workspaceId, userId.Value);

            // Pass null for filterOptions since they're not used anymore
            var tree = await _workspaceService.GetWorkspaceTreeV2Async(workspaceId, userId.Value, null);

            _logger.LogInformation("Successfully retrieved workspace tree V2 for workspaceId: {WorkspaceId} with {ItemCount} items (unfiltered)",
                workspaceId, tree.FlatData?.Count ?? 0);

            return Ok(new ResultOptions
            {
                Success = true,
                Message = "Workspace tree retrieved successfully",
                Status = StatusCodes.Status200OK,
                Object = tree
            });
        }

        /// <summary>
        /// Creates a new folder or updates an existing folder in a workspace
        /// </summary>
        /// <response code="200">Folder created/updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Workspace or folder not found</response>
        /// <response code="500">Internal server error</response>
        //[HttpPost("{workspaceId}/folders")]
        //[ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ResultOptions), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ResultOptions), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(typeof(ResultOptions), StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> UpsertFolder(int workspaceId, [FromBody] UpsertFolderRequest request)
        //{
        //    if (workspaceId <= 0)
        //    {
        //        _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
        //        return BadRequest(new ResultOptions
        //        {
        //            Success = false,
        //            Message = "Workspace ID must be a positive integer",
        //            Status = 400
        //        });
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        _logger.LogWarning("Invalid model state for upsert folder request");
        //        return BadRequest(new ResultOptions
        //        {
        //            Success = false,
        //            Message = "Invalid request data",
        //            Object = ModelState,
        //            Status = 400
        //        });
        //    }

        //    var userId = GetUserId();
        //    if (userId == null)
        //    {
        //        return Unauthorized("User ID not found in token");
        //    }

        //    var action = request.Id.HasValue ? "Updating" : "Creating";
        //    _logger.LogInformation("{Action} folder '{Name}' in workspace {WorkspaceId} for user {UserId}",
        //        action, request.Name, workspaceId, userId.Value);

        //    var result = await _workspaceService.UpsertFolderAsync(workspaceId, userId.Value, request);

        //    // Return appropriate status code based on result
        //    if (result.Success)
        //    {
        //        return Ok(result);
        //    }
        //    else
        //    {
        //        return StatusCode(result.Status ?? 500, result);
        //    }
        //}

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

            var userId = GetUserId();
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

            var userId = GetUserId();
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
        /// Batch upsert multiple workspace items with action-based operations
        /// Supports 7 actions: Create, Add, Move, MoveCross, UpdateFolder, Delete, Restore
        /// Use this for single item operations by passing an array with 1 element
        /// Pattern: Action-based API (Microsoft Graph style)
        ///
        /// Note: UpdateFolder only updates folder entities. Notes/Files use their own entity-specific APIs.
        /// </summary>
        /// <param name="workspaceId">Workspace ID from route</param>
        /// <param name="requests">List of workspace item requests with explicit actions</param>
        /// <returns>Batch operation results</returns>
        /// <response code="200">Workspace items processed successfully</response>
        /// <response code="400">Invalid input data or validation failed</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        /// <example>
        /// Example request body:
        /// [
        ///   { "action": "create", "itemType": 2, "parentId": null, "folderData": { "name": "New Folder" } },
        ///   { "action": "updatefolder", "id": 123, "folderData": { "name": "Updated Name", "color": "#FF5733" } },
        ///   { "action": "move", "id": 456, "parentId": null },
        ///   { "action": "delete", "id": 789 }
        /// ]
        /// </example>
        [HttpPost("{workspaceId}/items/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertWorkspaceItems(
            int workspaceId,
            [FromBody] List<UpsertWorkspaceItemRequest> requests)
        {
            // 1. Validate ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for batch workspace items request");
                return BadRequest(ModelState);
            }

            // 2. Validate non-empty list
            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("Empty batch request");
                return BadRequest("At least one workspace item is required");
            }

            // 3. Get userId from JWT token claims
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var userEmail = GetUserEmail();

            // 4. Set workspaceId, userId, and CreatedBy for all requests
            foreach (var request in requests)
            {
                request.WorkspaceId = workspaceId;
                request.UserId = userId.Value;
                request.CreatedBy = userEmail;
            }

            _logger.LogInformation(
                "Processing {Count} workspace items (actions: {Actions}) for workspace: {WorkspaceId}, user: {UserId}",
                requests.Count,
                string.Join(", ", requests.Select(r => r.Action.ToString())),
                workspaceId,
                userId.Value);

            // 5. Call WorkspaceItemService (action-based processing)
            var response = await _workspaceItemService.UpsertWorkspaceItemsAsync(
                requests,
                userId.Value,
                workspaceId);

            _logger.LogInformation(
                "Batch workspace items completed for workspace: {WorkspaceId}, user: {UserId}, Success: {Success}",
                workspaceId, userId.Value, response.Success);

            return Ok(response);
        }

        [HttpPost("{workspaceId}/items/move-cross")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MoveItemsCross(int workspaceId, [FromBody] MoveCrossRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            _logger.LogInformation(
                "Moving {Count} item(s) from workspace {WorkspaceId} to workspace {TargetWorkspaceId}",
                request.ItemIds.Count, workspaceId, request.TargetWorkspaceId);

            var result = await _workspaceItemService.MoveCrossAsync(request, userId.Value);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }
    }
}
