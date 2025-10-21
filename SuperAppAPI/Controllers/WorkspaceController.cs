using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperApp.Application.Features.Workspaces.Commands.AddItemToWorkspace;
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
                return CreatedAtAction(
                    nameof(GetWorkspaceItems),
                    new { workspaceId },
                    response);
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
        /// Gets all items in a workspace (placeholder for CreatedAtAction)
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <returns>List of workspace items</returns>
        /// <response code="200">Items retrieved successfully</response>
        /// <response code="404">Workspace not found</response>
        [HttpGet("{workspaceId}/items")]
        [ProducesResponseType(typeof(List<WorkspaceItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkspaceItems([FromRoute] int workspaceId)
        {
            try
            {
                // TODO: Implement GetWorkspaceItems query when needed
                _logger.LogInformation("Getting items for workspace {WorkspaceId}", workspaceId);
                
                // Placeholder - return empty list for now
                return Ok(new List<WorkspaceItemResponse>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for workspace {WorkspaceId}", workspaceId);
                return StatusCode(500, new { error = "An error occurred while retrieving workspace items" });
            }
        }
    }
}
