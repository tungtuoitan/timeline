using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
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
    //[Authorize] // TEMPORARY: Authorization disabled for development
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
        /// Gets all workspaces for the current user
        /// </summary>
        /// <response code="200">Workspaces retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<WorkspaceListResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUserWorkspaces()
        {
            // TEMPORARY: Using hardcoded userId while auth is disabled
            var userId = 1; // Hardcoded for development

            _logger.LogInformation("Retrieving all workspaces for userId: {UserId}", userId);

            var response = await _workspaceService.GetAllUserWorkspacesAsync(userId);

            _logger.LogInformation("Successfully retrieved {Count} workspaces for userId: {UserId}",
                response.Count, userId);

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

            // TEMPORARY: Using hardcoded userId while auth is disabled
            var userId = 1; // Hardcoded for development
            
            _logger.LogInformation("Retrieving workspace tree for workspaceId: {WorkspaceId}, userId: {UserId}", 
                workspaceId, userId);

            var response = await _workspaceService.GetWorkspaceTreeAsync(workspaceId, userId);

            _logger.LogInformation("Successfully retrieved workspace tree with {RootCount} root items for workspaceId: {WorkspaceId}",
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

            // TEMPORARY: Using hardcoded userId while auth is disabled
            var userId = 1; // Hardcoded for development

            var action = request.Id.HasValue ? "Updating" : "Creating";
            _logger.LogInformation("{Action} folder '{Name}' in workspace {WorkspaceId} for user {UserId}",
                action, request.Name, workspaceId, userId);

            var result = await _workspaceService.UpsertFolderAsync(workspaceId, userId, request);

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

            // TEMPORARY: Using hardcoded userId while auth is disabled
            var userId = 1; // Hardcoded for development

            _logger.LogInformation("Moving {Count} items in workspace {WorkspaceId} for user {UserId}",
                request.Items.Count, workspaceId, userId);

            var result = await _workspaceService.MoveItemsAsync(workspaceId, userId, request);

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

            // TEMPORARY: Using hardcoded userId while auth is disabled
            var userId = 1; // Hardcoded for development

            _logger.LogInformation("Deleting {Count} items in workspace {WorkspaceId} for user {UserId}",
                request.Items.Count, workspaceId, userId);

            var result = await _workspaceService.DeleteItemsAsync(workspaceId, userId, request);

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

            // TEMPORARY: Using hardcoded userId while auth is disabled
            var userId = 1; // Hardcoded for development

            _logger.LogInformation("Adding {ChildType} (ID: {ChildId}) to workspace {WorkspaceId} for user {UserId}",
                request.ChildType, request.ChildId, workspaceId, userId);

            var result = await _workspaceService.AddItemToWorkspaceAsync(workspaceId, userId, request);

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
    }
}
