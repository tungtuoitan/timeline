using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses; 
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.Projects
{
    /// <summary>
    /// Controller for managing notes operations
    /// </summary>
     [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : BaseAuthController
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(INoteService noteService, ILogger<NotesController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all notes with optional filtering for the authenticated user
        /// </summary>
        /// <param name="searchText">Optional search text filter</param>
        /// <param name="tagIds">Optional tag IDs filter</param>
        /// <param name="statusCode">Optional comma-separated status codes filter (e.g., "active,inactive")</param>
        /// <param name="deletedAt">Optional deleted status filter ("null" for active only, "notNull" for deleted only)</param>
        /// <param name="createdAtFrom">Optional created date from filter (ISO date string)</param>
        /// <param name="createdAtTo">Optional created date to filter (ISO date string)</param>
        /// <param name="ids">Optional comma-separated note IDs (e.g., "1,2,3") for restoring tabs</param>
        /// <param name="workspaceItemIds">Optional comma-separated workspace item IDs (e.g., "1,2,3") for keyword navigation</param>
        /// <returns>ResultOptions containing list of notes matching the criteria</returns>
        /// <response code="200">Notes retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNotes(
            [FromQuery] string? searchText = null,
            [FromQuery] List<int>? tagIds = null,
            [FromQuery] string? statusCode = null,
            [FromQuery] string? deletedAt = null,
            [FromQuery] string? createdAtFrom = null,
            [FromQuery] string? createdAtTo = null,
            [FromQuery] string? ids = null,
            [FromQuery] string? workspaceItemIds = null)
        {
            // Get userId from JWT token claims
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var userEmail = GetUserEmail();

            // Build filter options
            var filterOptions = new NoteFilterOptions
            {
                UserId = userId.Value,
                SearchText = searchText,
                TagIds = tagIds,
                StatusCodes = !string.IsNullOrEmpty(statusCode)
                    ? statusCode.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : null,
                DeletedAt = deletedAt,
                CreatedFrom = !string.IsNullOrEmpty(createdAtFrom) && DateTime.TryParse(createdAtFrom, out var parsedFrom)
                    ? parsedFrom
                    : null,
                CreatedTo = !string.IsNullOrEmpty(createdAtTo) && DateTime.TryParse(createdAtTo, out var parsedTo)
                    ? parsedTo
                    : null,
                Ids = !string.IsNullOrEmpty(ids)
                    ? ids.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
                    : null,
                WorkspaceItemIds = !string.IsNullOrEmpty(workspaceItemIds)
                    ? workspaceItemIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
                    : null
            };

            _logger.LogInformation(
                "Retrieving notes for userId: {UserId}, UserEmail: {UserEmail}, SearchText: {SearchText}, TagIds: {TagIds}, StatusCodes: {StatusCodes}, DeletedAt: {DeletedAt}, CreatedFrom: {CreatedFrom}, CreatedTo: {CreatedTo}",
                userId.Value, userEmail, filterOptions.SearchText,
                filterOptions.TagIds != null ? string.Join(",", filterOptions.TagIds) : "null",
                filterOptions.StatusCodes != null ? string.Join(",", filterOptions.StatusCodes) : "null",
                filterOptions.DeletedAt, filterOptions.CreatedFrom, filterOptions.CreatedTo);

            var response = await _noteService.GetNotesAsync(filterOptions);

            _logger.LogInformation("Successfully retrieved notes for user: {UserEmail}, Success: {Success}",
                userEmail, response.Success);

            return Ok(response);
        }

        /// <summary>
        /// Batch upsert multiple notes (create or update) in a single request
        /// Use this for single note operations by passing an array with 1 element
        /// </summary>
        /// <param name="requests">List of note upsert data</param>
        /// <returns>Batch operation results</returns>
        /// <response code="200">Notes upserted successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [HttpPost("batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertNotes([FromBody] List<UpsertNoteRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for batch upsert notes request");
                return BadRequest(ModelState);
            }

            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("Empty batch upsert request");
                return BadRequest("At least one note is required");
            }

            // Get userId from JWT token claims
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var userEmail = GetUserEmail();

            // Set userId and CreatedBy for all requests
            foreach (var request in requests)
            {
                request.UserId = userId.Value;
                request.CreatedBy = userEmail;

                // Clean up TagIds - remove invalid values (0 or negative)
                if (request.TagIds != null && request.TagIds.Any())
                {
                    request.TagIds = request.TagIds.Where(tagId => tagId > 0).Distinct().ToList();
                    if (!request.TagIds.Any())
                    {
                        request.TagIds = null;
                    }
                }
            }

            _logger.LogInformation("Batch upserting {Count} notes for user: {UserEmail}",
                requests.Count, userEmail);

            var response = await _noteService.UpsertNotesAsync(requests);

            _logger.LogInformation("Batch upsert notes completed for user: {UserEmail}, Success: {Success}",
                userEmail, response.Success);

            return Ok(response);
        }

        /// <summary>
        /// Gets a specific note by ID for the authenticated user
        /// </summary>
        /// <param name="id">Note ID</param>
        /// <returns>Note details if found and accessible</returns>
        /// <response code="200">Note retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Note not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNoteById(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid note ID provided: {NoteId}", id);
                throw new BadRequestException("Note ID must be a positive integer");
            }

            var userEmail = GetUserEmail();

            _logger.LogInformation("Retrieving note {NoteId} for user: {UserEmail}", id, userEmail);

            var response = await _noteService.GetNoteByIdAsync(id);

            _logger.LogInformation("Retrieved note {NoteId} for user: {UserEmail}, Success: {Success}", id, userEmail, response.Success);
            return Ok(response);
        }

      
        /// <summary>
        /// Hard deletes one or more notes for the authenticated user (permanently removes from database)
        /// For soft delete, use the Upsert endpoint with deletedAt timestamp
        /// </summary>
        /// <param name="id">Note ID to delete (supports comma-separated IDs, e.g., "1,2,3")</param>
        /// <returns>No content on successful deletion</returns>
        /// <response code="204">Note(s) deleted successfully</response>
        /// <response code="400">Invalid note ID(s)</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Note(s) not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteNote(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Empty note ID(s) provided for deletion");
                throw new BadRequestException("Note ID(s) must be provided");
            }

            var userEmail = GetUserEmail();

            _logger.LogInformation("Hard deleting note(s) {NoteIds} for user: {UserEmail}",
                id, userEmail);

            // Parse comma-separated IDs
            var noteIds = id.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            var response = await _noteService.DeleteNotesAsync(noteIds);

            _logger.LogInformation("Delete notes result for user: {UserEmail}, Success: {Success}", userEmail, response.Success);
            return Ok(response);
        }

    }
}
