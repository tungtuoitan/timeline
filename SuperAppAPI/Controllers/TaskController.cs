using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing task operations (Personal Productivity App)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskController> _logger;

        public TaskController(ITaskService taskService, ILogger<TaskController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all tasks with optional filtering
        /// Returns raw list (no tree building - frontend handles tree structure)
        /// </summary>
        /// <param name="projectIds">Optional: comma-separated project IDs (e.g., "1,2,3")</param>
        /// <param name="searchText">Optional search text filter for title/note</param>
        /// <param name="status">Optional status filter (e.g., "open", "done")</param>
        /// <param name="priority">Optional priority filter (e.g., "low", "medium", "high")</param>
        /// <param name="type">Optional type filter (e.g., "task", "milestone")</param>
        /// <param name="deletedAt">Optional deleted status filter ("null" for active only, "notNull" for deleted only)</param>
        /// <param name="ids">Optional comma-separated task IDs (e.g., "1,2,3")</param>
        /// <returns>ResultOptions containing list of tasks (raw list)</returns>
        /// <response code="200">Tasks retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTasks(
            [FromQuery] string? projectIds = null,
            [FromQuery] string? searchText = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] string? type = null,
            [FromQuery] string? deletedAt = null,
            [FromQuery] string? ids = null)
        {
            // Build filter options
            var filterOptions = new TaskFilterOptions
            {
                ProjectIds = !string.IsNullOrEmpty(projectIds)
                    ? projectIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                        .Where(id => id > 0)
                        .ToList()
                    : null,
                SearchText = searchText,
                Status = status,
                Priority = priority,
                Type = type,
                DeletedAt = deletedAt,
                Ids = !string.IsNullOrEmpty(ids)
                    ? ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                        .Where(id => id > 0)
                        .ToList()
                    : null
            };

            _logger.LogInformation(
                "Retrieving tasks with ProjectIds: {ProjectIds}, SearchText: {SearchText}, Status: {Status}, DeletedAt: {DeletedAt}",
                projectIds, searchText, status, deletedAt);

            var response = await _taskService.GetTasksAsync(filterOptions);

            _logger.LogInformation("Successfully retrieved tasks, Success: {Success}", response.Success);

            return Ok(response);
        }

        /// <summary>
        /// Batch upsert multiple tasks (create or update) in a single request
        /// Use this for single task operations by passing an array with 1 element
        /// Soft delete: pass deletedAt with a timestamp
        /// Restore: pass deletedAt as null for an existing task
        /// </summary>
        /// <param name="requests">List of task upsert data</param>
        /// <returns>Batch operation results</returns>
        /// <response code="200">Tasks upserted successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [HttpPost("batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertTasks([FromBody] List<UpsertTaskRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for batch upsert tasks request");
                return BadRequest(ModelState);
            }

            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("Empty batch upsert request");
                return BadRequest("At least one task is required");
            }

            _logger.LogInformation("Batch upserting {Count} tasks", requests.Count);

            var response = await _taskService.UpsertTasksAsync(requests);

            _logger.LogInformation("Batch upsert tasks completed, Success: {Success}", response.Success);

            return Ok(response);
        }
    }
}
