using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

                var response = await _workspaceService.GetWorkspaceTreeAsync(workspaceId, userId);

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
    }
}
