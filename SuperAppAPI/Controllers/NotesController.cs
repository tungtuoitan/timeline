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
    /// <summary>
    /// Controller for managing notes operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(INoteService noteService, ILogger<NotesController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        /// <summary>
        /// Get authenticated user ID from JWT claims
        /// </summary>
        private int? GetAuthenticatedUserId()
        {
            var userIdClaim = User.GetUserId();
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }
            return userId;
        }

        /// <summary>
        /// Get authenticated user email from JWT claims
        /// </summary>
        private string? GetAuthenticatedUserEmail()
        {
            return User.GetUserEmail();
        }

        /// <summary>
        /// Gets all notes with optional filtering for the authenticated user
        /// </summary>
        /// <param name="searchText">Optional search text filter</param>
        /// <param name="tagIds">Optional tag IDs filter</param>
        /// <returns>ResultOptions containing list of notes matching the criteria</returns>
        /// <response code="200">Notes retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNotes(
            [FromQuery] string? searchText = null,
            [FromQuery] List<int>? tagIds = null)
        {
            // Get userId from JWT token claims
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var userEmail = GetAuthenticatedUserEmail();

            _logger.LogInformation(
                "Retrieving notes for userId: {UserId}, UserEmail: {UserEmail}, SearchText: {SearchText}, TagIds: {TagIds}",
                userId.Value, userEmail, searchText, tagIds != null ? string.Join(",", tagIds) : "null");

            var response = await _noteService.GetNotesAsync(userId.Value, searchText, tagIds);

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
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var userEmail = GetAuthenticatedUserEmail();

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

            var userEmail = GetAuthenticatedUserEmail();

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

            var userEmail = GetAuthenticatedUserEmail();

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
