using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing workspace list operations (ws.workspaces)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    // TEMPORARY: [Authorize] disabled while authentication is disabled for development
    // [Authorize]
    public class WorkspaceListController : ControllerBase
    {
        private readonly IWorkspaceListService _workspaceListService;
        private readonly ILogger<WorkspaceListController> _logger;

        public WorkspaceListController(IWorkspaceListService workspaceListService, ILogger<WorkspaceListController> logger)
        {
            _workspaceListService = workspaceListService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all workspaces with optional filtering for the authenticated user
        /// </summary>
        /// <param name="getAll">Get all workspaces flag (admin only)</param>
        /// <param name="searchText">Optional search text filter</param>
        /// <returns>ResultOptions containing list of workspaces matching the criteria</returns>
        /// <response code="200">Workspaces retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetWorkspaces(
            [FromQuery] bool getAll = false, 
            [FromQuery] string? searchText = null)
        {
            var userEmail = User.GetUserEmail();

            _logger.LogInformation(
                "Retrieving workspaces for user: {UserEmail}, GetAll: {GetAll}, SearchText: {SearchText}",
                userEmail, getAll, searchText);

            var response = await _workspaceListService.GetWorkspacesAsync(getAll, searchText);

            _logger.LogInformation("Successfully retrieved workspaces for user: {UserEmail}, Success: {Success}",
                userEmail, response.Success);

            return Ok(response);
        }

        /// <summary>
        /// Creates a new workspace or updates an existing workspace (upsert)
        /// </summary>
        /// <param name="request">Workspace upsert data</param>
        /// <returns>Created or updated workspace</returns>
        /// <response code="200">Workspace updated successfully</response>
        /// <response code="201">Workspace created successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertWorkspace([FromBody] UpsertWorkspaceRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for upsert workspace request");
                return BadRequest(ModelState);
            }

            // TEMPORARY: Using hardcoded email while auth is disabled
            var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

            _logger.LogInformation("Upserting workspace with ID: {WorkspaceId}, Name: '{WorkspaceName}' for user: {UserEmail}",
                request.Id, request.Name, userEmail);

            var response = await _workspaceListService.UpsertWorkspaceAsync(request);

            _logger.LogInformation("Workspace upserted for user: {UserEmail}, Success: {Success}",
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

            // TEMPORARY: Using hardcoded email while auth is disabled
            var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

            _logger.LogInformation("Retrieving workspace {WorkspaceId} for user: {UserEmail}", id, userEmail);

            var response = await _workspaceListService.GetWorkspaceByIdAsync(id);

            _logger.LogInformation("Retrieved workspace {WorkspaceId} for user: {UserEmail}, Success: {Success}", id, userEmail, response.Success);
            return Ok(response);
        }

      
        /// <summary>
        /// Deletes one or more workspaces for the authenticated user
        /// </summary>
        /// <param name="id">Workspace ID to delete (supports comma-separated IDs, e.g., "1,2,3")</param>
        /// <param name="isHardDelete">Hard delete flag: true = permanently delete, false = soft delete (default)</param>
        /// <returns>No content on successful deletion</returns>
        /// <response code="204">Workspace(s) deleted successfully</response>
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
        public async Task<IActionResult> DeleteWorkspace(string id, [FromQuery] bool isHardDelete = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Empty workspace ID(s) provided for deletion");
                throw new BadRequestException("Workspace ID(s) must be provided");
            }

            // TEMPORARY: Using hardcoded email while auth is disabled
            var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

            _logger.LogInformation("Deleting workspace(s) {WorkspaceIds} for user: {UserEmail} (HardDelete: {IsHardDelete})", 
                id, userEmail, isHardDelete);

            // Parse comma-separated IDs
            var workspaceIds = id.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            var response = await _workspaceListService.DeleteWorkspacesAsync(workspaceIds, isHardDelete);

            _logger.LogInformation("Delete workspaces result for user: {UserEmail}, Success: {Success}", userEmail, response.Success);
            return Ok(response);
        }

        /// <summary>
        /// Restores one or more deleted workspaces for the authenticated user (undo soft delete)
        /// </summary>
        /// <param name="id">Workspace ID to restore (supports comma-separated IDs, e.g., "1,2,3")</param>
        /// <returns>No content on successful restoration</returns>
        /// <response code="204">Workspace(s) restored successfully</response>
        /// <response code="400">Invalid workspace ID(s)</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Workspace(s) not found or not deleted</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("undo/{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UndoDeleteWorkspace(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Empty workspace ID(s) provided for undo deletion");
                throw new BadRequestException("Workspace ID(s) must be provided");
            }

            // TEMPORARY: Using hardcoded email while auth is disabled
            var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

            _logger.LogInformation("Restoring deleted workspace(s) {WorkspaceIds} for user: {UserEmail}", id, userEmail);

            // Parse comma-separated IDs
            var workspaceIds = id.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            var response = await _workspaceListService.UndoDeleteWorkspacesAsync(workspaceIds);

            _logger.LogInformation("Restore workspaces result for user: {UserEmail}, Success: {Success}", userEmail, response.Success);
            return Ok(response);
        }
    }
}
