using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing project operations (Personal Productivity App)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectController> _logger;

        public ProjectController(IProjectService projectService, ILogger<ProjectController> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all projects with optional filtering
        /// </summary>
        /// <param name="searchText">Optional search text filter for name/description</param>
        /// <param name="status">Optional status filter (e.g., "open", "closed")</param>
        /// <param name="deletedAt">Optional deleted status filter ("null" for active only, "notNull" for deleted only)</param>
        /// <param name="ids">Optional comma-separated project IDs (e.g., "1,2,3")</param>
        /// <returns>ResultOptions containing list of projects</returns>
        /// <response code="200">Projects retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProjects(
            [FromQuery] string? searchText = null,
            [FromQuery] string? status = null,
            [FromQuery] string? deletedAt = null,
            [FromQuery] string? ids = null)
        {
            // Build filter options
            var filterOptions = new ProjectFilterOptions
            {
                SearchText = searchText,
                Status = status,
                DeletedAt = deletedAt,
                Ids = !string.IsNullOrEmpty(ids)
                    ? ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                        .Where(id => id > 0)
                        .ToList()
                    : null
            };

            _logger.LogInformation(
                "Retrieving projects with SearchText: {SearchText}, Status: {Status}, DeletedAt: {DeletedAt}",
                searchText, status, deletedAt);

            var response = await _projectService.GetProjectsAsync(filterOptions);

            _logger.LogInformation("Successfully retrieved projects, Success: {Success}", response.Success);

            return Ok(response);
        }

        /// <summary>
        /// Batch upsert multiple projects (create or update) in a single request
        /// Use this for single project operations by passing an array with 1 element
        /// Soft delete: pass deletedAt with a timestamp
        /// Restore: pass deletedAt as null for an existing project
        /// </summary>
        /// <param name="requests">List of project upsert data</param>
        /// <returns>Batch operation results</returns>
        /// <response code="200">Projects upserted successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [HttpPost("batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertProjects([FromBody] List<UpsertProjectRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for batch upsert projects request");
                return BadRequest(ModelState);
            }

            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("Empty batch upsert request");
                return BadRequest("At least one project is required");
            }

            _logger.LogInformation("Batch upserting {Count} projects", requests.Count);

            var response = await _projectService.UpsertProjectsAsync(requests);

            _logger.LogInformation("Batch upsert projects completed, Success: {Success}", response.Success);

            return Ok(response);
        }
    }
}
