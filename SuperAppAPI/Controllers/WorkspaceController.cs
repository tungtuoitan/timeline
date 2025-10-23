using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperApp.Application.Features.Workspaces.Commands.AddItemToWorkspace;
using SuperApp.Application.Features.Workspaces.Commands.UpdateWorkspaceItem;
using SuperApp.Application.Features.Workspaces.Commands.MoveWorkspaceItem;
using SuperApp.Application.Features.Workspaces.Commands.RemoveWorkspaceItem;
using SuperApp.Application.Features.Workspaces.Queries.GetWorkspaceTree;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing workspace operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // TEMPORARY: Authorization disabled for development
    public class WorkspaceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<WorkspaceController> _logger;

        public WorkspaceController(IMediator mediator, ILogger<WorkspaceController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets workspace information with its complete hierarchical tree (tags, notes, and files)
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <returns>Workspace details with hierarchical tree structure (polymorphic: tags, notes, files)</returns>
        /// <remarks>
        /// Returns a workspace with its complete hierarchical structure including:
        /// - Tags organized in parent-child relationships
        /// - Notes attached to tags
        /// - Files attached to tags
        /// 
        /// The tree structure is polymorphic, meaning each node can be a tag, note, or file.
        /// Root items (items without parent) are returned at the top level.
        /// </remarks>
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
            try
            {
                if (workspaceId <= 0)
                {
                    _logger.LogWarning("Invalid workspace ID provided: {WorkspaceId}", workspaceId);
                    return BadRequest(new { Message = "Workspace ID must be a positive integer" });
                }

                // TEMPORARY: Using hardcoded userId while auth is disabled
                var userId = 1; // Hardcoded for development
                
                _logger.LogInformation("Retrieving workspace tree for workspaceId: {WorkspaceId}, userId: {UserId}", 
                    workspaceId, userId);

                var query = new GetWorkspaceTreeQuery(workspaceId, userId);
                var response = await _mediator.Send(query);

                _logger.LogInformation("Successfully retrieved workspace tree with {RootCount} root items for workspaceId: {WorkspaceId}",
                    response?.Items?.Count ?? 0, workspaceId);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get workspace tree: {WorkspaceId}", workspaceId);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for workspace tree: {WorkspaceId}", workspaceId);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving workspace tree for workspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving workspace tree" });
            }
        }

        /// <summary>
        /// Adds a tag or note to a workspace (creates hierarchy/relationship)
        /// </summary>
        /// <param name="workspaceId">The workspace ID to add the item to</param>
        /// <param name="request">Details of the item to add (tag or note)</param>
        /// <returns>The created workspace item with details</returns>
        /// <remarks>
        /// Each item in a workspace is unique based on: WorkspaceId + ParentTagId + ChildType + ChildId.
        /// If you try to add the same item twice, you'll get a 400 Bad Request error.
        /// 
        /// Example scenarios:
        /// - Adding tag 5 to workspace 1 under root: Allowed (first time)
        /// - Adding tag 5 to workspace 1 under root again: REJECTED (duplicate)
        /// - Adding tag 5 to workspace 1 under tag 3: Allowed (different parent)
        /// - Adding note 10 to workspace 1 under tag 5: Allowed (different type)
        /// </remarks>
        /// <response code="201">Item added to workspace successfully</response>
        /// <response code="400">Invalid request data or duplicate item</response>
        /// <response code="401">Unauthorized - user doesn't have access to workspace</response>
        /// <response code="404">Workspace, tag, or note not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{workspaceId}/items")]
        [ProducesResponseType(typeof(WorkspaceItemResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddItemToWorkspace(
            [FromRoute] int workspaceId,
            [FromBody] AddItemToWorkspaceRequest request)
        {
            try
            {
                // TEMPORARY: Using hardcoded userId while auth is disabled
                // TODO: Extract from JWT token when authentication is enabled
                var userId = 1;

                _logger.LogInformation(
                    "Adding {ChildType} item (ID: {ChildId}) to workspace {WorkspaceId} by user {UserId}",
                    request.ChildType, request.ChildId, workspaceId, userId);

                // Create command with route parameter and request data
                var command = new AddItemToWorkspaceCommand
                {
                    WorkspaceId = workspaceId,
                    UserId = userId,
                    ParentTagId = request.ParentTagId,
                    ChildType = request.ChildType,
                    ChildId = request.ChildId,
                    TagName = request.TagName,  // For auto-creating tags
                    RelationshipType = request.RelationshipType,
                    Label = request.Label,
                    Notes = request.Notes,
                    SortOrder = request.SortOrder,
                    Color = request.Color,
                    Icon = request.Icon
                };

                // Send command via MediatR
                var response = await _mediator.Send(command);

                _logger.LogInformation(
                    "Successfully added item {ItemId} to workspace {WorkspaceId}",
                    response.ItemId, workspaceId);

                // Return 201 Created with Location header
                return Created($"/api/workspace/{workspaceId}/tree", response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized attempt to add item to workspace {WorkspaceId} by user {UserId}",
                    workspaceId, 1);
                return Unauthorized(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Invalid request to add item to workspace {WorkspaceId}: {ErrorMessage}",
                    workspaceId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error adding item to workspace {WorkspaceId}",
                    workspaceId);
                return StatusCode(500, new { error = "An error occurred while adding item to workspace" });
            }
        }

        /// <summary>
        /// Updates a workspace item (label, notes, color, icon, sort order)
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <param name="itemId">The workspace item ID to update</param>
        /// <param name="request">Update request with fields to change</param>
        /// <returns>Updated workspace item details</returns>
        /// <remarks>
        /// You can update any combination of: Label, Notes, Color, Icon, SortOrder.
        /// At least one field must be provided. Null/empty fields will be ignored.
        /// 
        /// Example:
        /// - Update only label: { "label": "New Label" }
        /// - Update label and color: { "label": "New Label", "color": "#FF5733" }
        /// - Update all fields: { "label": "...", "notes": "...", "color": "...", "icon": "...", "sortOrder": 5 }
        /// </remarks>
        /// <response code="200">Item updated successfully</response>
        /// <response code="400">Invalid request data (no fields provided or invalid values)</response>
        /// <response code="401">Unauthorized - user doesn't have access to workspace</response>
        /// <response code="404">Workspace or item not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("{workspaceId}/items/{itemId}")]
        [ProducesResponseType(typeof(UpdateWorkspaceItemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateWorkspaceItem(
            [FromRoute] int workspaceId,
            [FromRoute] long itemId,
            [FromBody] UpdateWorkspaceItemRequest request)
        {
            try
            {
                // TEMPORARY: Using hardcoded userId while auth is disabled
                // TODO: Extract from JWT token when authentication is enabled
                var userId = 1;

                _logger.LogInformation(
                    "Updating workspace item {ItemId} in workspace {WorkspaceId} by user {UserId}",
                    itemId, workspaceId, userId);

                // Create command
                var command = new UpdateWorkspaceItemCommand
                {
                    ItemId = (int)itemId,
                    UserId = userId,
                    Label = request.Label,
                    Notes = request.Notes,
                    Color = request.Color,
                    Icon = request.Icon,
                    SortOrder = request.SortOrder
                };

                // Send command via MediatR
                var response = await _mediator.Send(command);

                _logger.LogInformation(
                    "Successfully updated workspace item {ItemId} in workspace {WorkspaceId}",
                    itemId, workspaceId);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized attempt to update item {ItemId} in workspace {WorkspaceId} by user {UserId}",
                    itemId, workspaceId, 1);
                return Unauthorized(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Invalid request to update item {ItemId} in workspace {WorkspaceId}: {ErrorMessage}",
                    itemId, workspaceId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating workspace item {ItemId} in workspace {WorkspaceId}",
                    itemId, workspaceId);
                return StatusCode(500, new { error = "An error occurred while updating workspace item" });
            }
        }

        /// <summary>
        /// Moves a workspace item to a different parent or changes its position
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <param name="itemId">The workspace item ID to move</param>
        /// <param name="request">Move request with new parent and/or sort order</param>
        /// <returns>Updated workspace item details after move</returns>
        /// <remarks>
        /// Move operations:
        /// - Move to different parent: Provide newParentTagId (null for root level)
        /// - Change position under same parent: Provide sortOrder only
        /// - Move and reposition: Provide both newParentTagId and sortOrder
        /// 
        /// Examples:
        /// - Move to root: { "newParentTagId": null }
        /// - Move to tag 5: { "newParentTagId": 5 }
        /// - Move to tag 5 and position 3: { "newParentTagId": 5, "sortOrder": 3 }
        /// - Reorder only: { "sortOrder": 10 }
        /// </remarks>
        /// <response code="200">Item moved successfully</response>
        /// <response code="400">Invalid request data (cycle detection, invalid parent, etc.)</response>
        /// <response code="401">Unauthorized - user doesn't have access to workspace</response>
        /// <response code="404">Workspace, item, or target parent not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPatch("{workspaceId}/items/{itemId}/move")]
        [ProducesResponseType(typeof(UpdateWorkspaceItemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MoveWorkspaceItem(
            [FromRoute] int workspaceId,
            [FromRoute] long itemId,
            [FromBody] MoveWorkspaceItemRequest request)
        {
            try
            {
                // TEMPORARY: Using hardcoded userId while auth is disabled
                // TODO: Extract from JWT token when authentication is enabled
                var userId = 1;

                _logger.LogInformation(
                    "Moving workspace item {ItemId} in workspace {WorkspaceId} to parent {NewParentTagId} by user {UserId}",
                    itemId, workspaceId, request.NewParentTagId ?? -1, userId);

                // Create command
                var command = new MoveWorkspaceItemCommand
                {
                    ItemId = (int)itemId,
                    UserId = userId,
                    NewParentTagId = request.NewParentTagId,
                    SortOrder = request.SortOrder
                };

                // Send command via MediatR
                var response = await _mediator.Send(command);

                _logger.LogInformation(
                    "Successfully moved workspace item {ItemId} in workspace {WorkspaceId}",
                    itemId, workspaceId);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized attempt to move item {ItemId} in workspace {WorkspaceId} by user {UserId}",
                    itemId, workspaceId, 1);
                return Unauthorized(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Invalid request to move item {ItemId} in workspace {WorkspaceId}: {ErrorMessage}",
                    itemId, workspaceId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error moving workspace item {ItemId} in workspace {WorkspaceId}",
                    itemId, workspaceId);
                return StatusCode(500, new { error = "An error occurred while moving workspace item" });
            }
        }

        /// <summary>
        /// Removes an item from workspace (soft delete)
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <param name="itemId">The workspace item ID to remove</param>
        /// <returns>NoContent on success</returns>
        /// <remarks>
        /// This endpoint removes the workspace_items relationship only.
        /// It does NOT delete the actual tag or note from the database.
        ///
        /// By default, it also removes all descendants (children, grandchildren, etc.)
        /// from the workspace hierarchy.
        ///
        /// Examples:
        /// - DELETE /api/workspace/1/items/123
        ///   Removes item 123 and all its children from workspace 1
        /// </remarks>
        /// <response code="204">Item removed successfully</response>
        /// <response code="401">Unauthorized - user doesn't have access to workspace</response>
        /// <response code="404">Workspace or item not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{workspaceId}/items/{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveWorkspaceItem(
            [FromRoute] int workspaceId,
            [FromRoute] long itemId)
        {
            try
            {
                // TEMPORARY: Using hardcoded userId while auth is disabled
                // TODO: Extract from JWT token when authentication is enabled
                var userId = 1;

                _logger.LogInformation(
                    "Removing workspace item {ItemId} from workspace {WorkspaceId} by user {UserId}",
                    itemId, workspaceId, userId);

                // Create command
                var command = new RemoveWorkspaceItemCommand
                {
                    ItemId = (int)itemId,
                    UserId = userId,
                    DeleteDescendants = true // Always delete descendants
                };

                // Send command via MediatR
                var success = await _mediator.Send(command);

                if (!success)
                {
                    _logger.LogWarning(
                        "Workspace item {ItemId} not found in workspace {WorkspaceId}",
                        itemId, workspaceId);
                    return NotFound(new { error = "Workspace item not found" });
                }

                _logger.LogInformation(
                    "Successfully removed workspace item {ItemId} from workspace {WorkspaceId}",
                    itemId, workspaceId);

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized attempt to remove item {ItemId} from workspace {WorkspaceId} by user {UserId}",
                    itemId, workspaceId, 1);
                return Unauthorized(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Invalid request to remove item {ItemId} from workspace {WorkspaceId}: {ErrorMessage}",
                    itemId, workspaceId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error removing workspace item {ItemId} from workspace {WorkspaceId}",
                    itemId, workspaceId);
                return StatusCode(500, new { error = "An error occurred while removing workspace item" });
            }
        }
    }
}
