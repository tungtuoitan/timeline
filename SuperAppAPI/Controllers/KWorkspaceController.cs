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
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KWorkspaceController : ControllerBase
    {
        private readonly IKWorkspaceService _KworkspaceService;
        private readonly IKWorkspaceItemService _workspaceItemService;
        private readonly ILogger<KWorkspaceController> _logger;

        public KWorkspaceController(
            IKWorkspaceService workspaceService,
            IKWorkspaceItemService workspaceItemService,
            ILogger<KWorkspaceController> logger)
        {
            _KworkspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
            _workspaceItemService = workspaceItemService ?? throw new ArgumentNullException(nameof(workspaceItemService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private int? GetAuthenticatedUserId()
        {
            var userIdClaim = User.GetUserId();
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }

        /// <summary>Gets all workspaces for the current user</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<WsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUserWorkspaces(
            [FromQuery] string? statusCode = null,
            [FromQuery] string? deletedAt = null,
            [FromQuery] string? createdAtFrom = null,
            [FromQuery] string? createdAtTo = null)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var filterOptions = new FilterOptions
            {
                StatusCodes = !string.IsNullOrEmpty(statusCode)
                    ? statusCode.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : null,
                DeletedAt = deletedAt,
                CreatedFrom = !string.IsNullOrEmpty(createdAtFrom) && DateTime.TryParse(createdAtFrom, out var parsedFrom) ? parsedFrom : null,
                CreatedTo = !string.IsNullOrEmpty(createdAtTo) && DateTime.TryParse(createdAtTo, out var parsedTo) ? parsedTo : null
            };

            var response = await _KworkspaceService.GetAllUserWorkspacesAsync(userId.Value, filterOptions);
            return Ok(response);
        }

        /// <summary>
        /// Gets workspace tree V2 — flat list of nodes (self-contained, no entity joins)
        /// Frontend filters and builds hierarchy from parentId
        /// </summary>
        [HttpGet("{workspaceId}/tree/v2")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWorkspaceTreeV2(int workspaceId)
        {
            if (workspaceId <= 0)
                throw new BadRequestException("Workspace ID must be a positive integer");

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Retrieving workspace tree V2 for workspaceId: {WorkspaceId}, userId: {UserId}",
                workspaceId, userId.Value);

            var tree = await _KworkspaceService.GetWorkspaceTreeV2Async(workspaceId, userId.Value, null);

            _logger.LogInformation("Retrieved workspace tree V2 with {Count} items for workspaceId: {WorkspaceId}",
                tree.FlatData?.Count ?? 0, workspaceId);

            return Ok(new ResultOptions
            {
                Success = true,
                Message = "Workspace tree retrieved successfully",
                Status = StatusCodes.Status200OK,
                Object = tree
            });
        }

        /// <summary>Moves multiple workspace items by workspace_item IDs</summary>
        [HttpPatch("{workspaceId}/items/move")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> MoveItems(int workspaceId, [FromBody] KMoveItemsRequest request)
        {
            if (workspaceId <= 0)
                return BadRequest(new ResultOptions { Success = false, Message = "Workspace ID must be a positive integer", Status = 400 });

            if (!ModelState.IsValid)
                return BadRequest(new ResultOptions { Success = false, Message = "Invalid request data", Object = ModelState, Status = 400 });

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Moving {Count} items in workspace {WorkspaceId}", request.ItemIds.Count, workspaceId);

            var result = await _KworkspaceService.MoveItemsAsync(workspaceId, userId.Value, request);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }

        /// <summary>Deletes multiple workspace items (and descendants) by workspace_item IDs</summary>
        [HttpDelete("{workspaceId}/items")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteItems(int workspaceId, [FromBody] KDeleteItemsRequest request)
        {
            if (workspaceId <= 0)
                return BadRequest(new ResultOptions { Success = false, Message = "Workspace ID must be a positive integer", Status = 400 });

            if (!ModelState.IsValid)
                return BadRequest(new ResultOptions { Success = false, Message = "Invalid request data", Object = ModelState, Status = 400 });

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Deleting {Count} items in workspace {WorkspaceId}", request.ItemIds.Count, workspaceId);

            var result = await _KworkspaceService.DeleteItemsAsync(workspaceId, userId.Value, request);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }

        /// <summary>
        /// Batch upsert workspace item nodes with explicit actions.
        /// Actions: Create, Update, Move, MoveCross, Delete, Restore
        /// Example: [{ "action": "create", "nodeData": { "name": "My Node" } }]
        /// </summary>
        [HttpPost("{workspaceId}/items/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertWorkspaceItems(
            int workspaceId,
            [FromBody] List<KUpsertWorkspaceItemRequest> requests)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (requests == null || !requests.Any())
                return BadRequest("At least one workspace item is required");

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var userEmail = User.GetUserEmail();

            foreach (var request in requests)
            {
                if (request.Action != KWorkspaceItemAction.MoveCross)
                    request.WorkspaceId = workspaceId;

                request.UserId = userId.Value;
                request.CreatedBy = userEmail;
            }

            _logger.LogInformation("Processing {Count} workspace items (actions: {Actions}) for workspace: {WorkspaceId}",
                requests.Count,
                string.Join(", ", requests.Select(r => r.Action.ToString())),
                workspaceId);

            var response = await _workspaceItemService.UpsertWorkspaceItemsAsync(requests, userId.Value, workspaceId);

            return Ok(response);
        }
    }
}
