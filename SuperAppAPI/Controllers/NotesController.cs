using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using SuperAppAPI.Extensions;
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
    // TEMPORARY: [Authorize] disabled while authentication is disabled for development
    // [Authorize]
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
        /// Gets all notes with optional filtering for the authenticated user
        /// </summary>
        /// <param name="getAll">Get all notes flag (admin only)</param>
        /// <param name="searchText">Optional search text filter</param>
        /// <param name="tagIds">Optional tag IDs filter</param>
        /// <returns>List of notes matching the criteria</returns>
        /// <response code="200">Notes retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<NoteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNotes(
            [FromQuery] bool getAll = false, 
            [FromQuery] string? searchText = null, 
            [FromQuery] List<int>? tagIds = null)
        {
            var userEmail = User.GetUserEmail();

            _logger.LogInformation(
                "Retrieving notes for user: {UserEmail}, GetAll: {GetAll}, SearchText: {SearchText}, TagIds: {TagIds}",
                userEmail, getAll, searchText, tagIds != null ? string.Join(",", tagIds) : "null");

            var response = await _noteService.GetNotesAsync(getAll, searchText, tagIds);

            _logger.LogInformation("Successfully retrieved {NoteCount} notes for user: {UserEmail}",
                response?.Count ?? 0, userEmail);

            return Ok(response);
        }

        /// <summary>
        /// Creates a new note or updates an existing note (upsert)
        /// </summary>
        /// <param name="request">Note upsert data</param>
        /// <returns>Created or updated note</returns>
        /// <response code="200">Note updated successfully</response>
        /// <response code="201">Note created successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertNote([FromBody] UpsertNoteRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for upsert note request");
                return BadRequest(ModelState);
            }

            // TEMPORARY: Using hardcoded email while auth is disabled
            var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

            // Set the CreatedBy from the authenticated user
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

            _logger.LogInformation("Upserting note with ID: {NoteId}, Name: '{NoteName}' for user: {UserEmail}",
                request.Id, request.Name, userEmail);

            var response = await _noteService.UpsertNoteAsync(request);

            if (request.Id == 0)
            {
                _logger.LogInformation("Note created successfully with ID: {NoteId} for user: {UserEmail}",
                    response.Id, userEmail);
                return CreatedAtAction(nameof(GetNoteById), new { id = response.Id }, response);
            }
            else
            {
                _logger.LogInformation("Note updated successfully with ID: {NoteId} for user: {UserEmail}",
                    response.Id, userEmail);
                return Ok(response);
            }
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
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
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

            // TEMPORARY: Using hardcoded email while auth is disabled
            var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

            _logger.LogInformation("Retrieving note {NoteId} for user: {UserEmail}", id, userEmail);

            var note = await _noteService.GetNoteByIdAsync(id);

            _logger.LogInformation("Successfully retrieved note {NoteId} for user: {UserEmail}", id, userEmail);
            return Ok(note);
        }

      
        /// <summary>
        /// Deletes one or more notes for the authenticated user
        /// </summary>
        /// <param name="id">Note ID to delete (supports comma-separated IDs, e.g., "1,2,3")</param>
        /// <returns>No content on successful deletion</returns>
        /// <response code="204">Note(s) deleted successfully</response>
        /// <response code="400">Invalid note ID(s)</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Note(s) not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
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

            // TEMPORARY: Using hardcoded email while auth is disabled
            var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

            _logger.LogInformation("Deleting note(s) {NoteIds} for user: {UserEmail}", id, userEmail);

            // Parse comma-separated IDs
            var noteIds = id.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            var success = await _noteService.DeleteNotesAsync(noteIds);

            if (!success)
            {
                _logger.LogWarning("Note(s) {NoteIds} not found for deletion for user: {UserEmail}", id, userEmail);
                throw new NotFoundException($"Note(s) with ID(s) {id} not found");
            }

            _logger.LogInformation("Successfully deleted note(s) {NoteIds} for user: {UserEmail}", id, userEmail);
            return NoContent();
        }

        /// <summary>
        /// Restores one or more deleted notes for the authenticated user (undo soft delete)
        /// </summary>
        /// <param name="id">Note ID to restore (supports comma-separated IDs, e.g., "1,2,3")</param>
        /// <returns>No content on successful restoration</returns>
        /// <response code="204">Note(s) restored successfully</response>
        /// <response code="400">Invalid note ID(s)</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Note(s) not found or not deleted</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("undo/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UndoDeleteNote(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Empty note ID(s) provided for undo deletion");
                throw new BadRequestException("Note ID(s) must be provided");
            }

            // TEMPORARY: Using hardcoded email while auth is disabled
            var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";

            _logger.LogInformation("Restoring deleted note(s) {NoteIds} for user: {UserEmail}", id, userEmail);

            // Parse comma-separated IDs
            var noteIds = id.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            var success = await _noteService.UndoDeleteNotesAsync(noteIds);

            if (!success)
            {
                _logger.LogWarning("Note(s) {NoteIds} not found or not deleted for user: {UserEmail}", id, userEmail);
                throw new NotFoundException($"Deleted note(s) with ID(s) {id} not found");
            }

            _logger.LogInformation("Successfully restored note(s) {NoteIds} for user: {UserEmail}", id, userEmail);
            return NoContent();
        }

    }
}
