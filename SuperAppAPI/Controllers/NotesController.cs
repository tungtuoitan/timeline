using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    //[Authorize] // Restore authorization for all endpoints - security critical!
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(INoteService noteService, ILogger<NotesController> logger)
        {
            _noteService = noteService ?? throw new ArgumentNullException(nameof(noteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNotes([FromQuery] bool getAll = false, [FromQuery] string? searchText = null, [FromQuery] List<int>? tagIds = null)
        {
            try
            {
                // TEMPORARY: Using hardcoded email while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Retrieving notes for user: {UserEmail}, GetAll: {GetAll}, SearchText: {SearchText}, TagIds: {TagIds}",
                    userEmail, getAll, searchText, tagIds != null ? string.Join(",", tagIds) : "null");

                var response = await _noteService.GetNotesAsync(getAll, searchText, tagIds);

                _logger.LogInformation("Successfully retrieved {NoteCount} notes for user: {UserEmail}",
                    response?.Count ?? 0, userEmail);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get notes");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for get notes");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving notes");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving notes" });
            }
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
            try
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
                    request.NoteId, request.Name, userEmail);

                var response = await _noteService.UpsertNoteAsync(request);

                if (request.NoteId == 0)
                {
                    _logger.LogInformation("Note created successfully with ID: {NoteId} for user: {UserEmail}", 
                        response.NoteId, userEmail);
                    return CreatedAtAction(nameof(GetNoteById), new { id = response.NoteId }, response);
                }
                else
                {
                    _logger.LogInformation("Note updated successfully with ID: {NoteId} for user: {UserEmail}", 
                        response.NoteId, userEmail);
                    return Ok(response);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for note upsert");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for note upsert");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while upserting note");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while processing the note" });
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
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid note ID provided: {NoteId}", id);
                    return BadRequest(new { Message = "Note ID must be a positive integer" });
                }

                // TEMPORARY: Using hardcoded email while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                //if (string.IsNullOrEmpty(userEmail))
                //{
                //    _logger.LogWarning("Failed to extract user email from token");
                //    return Unauthorized(new { Message = "Invalid token claims" });
                //}

                _logger.LogInformation("Retrieving note {NoteId} for user: {UserEmail}", id, userEmail);

                var note = await _noteService.GetNoteByIdAsync(id);

                _logger.LogInformation("Successfully retrieved note {NoteId} for user: {UserEmail}", id, userEmail);
                return Ok(note);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get note by ID: {NoteId}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Note {NoteId} not found", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for note {NoteId}", id);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving note {NoteId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving the note" });
            }
        }

        /// <summary>
        /// Updates an existing note (alternative endpoint with PUT {id})
        /// </summary>
        /// <param name="id">Note ID to update</param>
        /// <param name="request">Note data for update</param>
        /// <returns>Updated note details</returns>
        /// <response code="200">Note updated successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Note not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateNote(int id, [FromBody] UpsertNoteRequest request)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid note ID provided for update: {NoteId}", id);
                return BadRequest(new { Message = "Note ID must be positive" });
            }

            // Set the NoteId from URL parameter
            request.NoteId = id;

            return await UpsertNote(request);
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
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Empty note ID(s) provided for deletion");
                    return BadRequest(new { Message = "Note ID(s) must be provided" });
                }

                // TEMPORARY: Using hardcoded email while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                //if (string.IsNullOrEmpty(userEmail))
                //{
                //    _logger.LogWarning("Failed to extract user email from token");
                //    return Unauthorized(new { Message = "Invalid token claims" });
                //}

                _logger.LogInformation("Deleting note(s) {NoteIds} for user: {UserEmail}", id, userEmail);

                // Parse comma-separated IDs
                var noteIds = id.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();

                var success = await _noteService.DeleteNotesAsync(noteIds);

                if (!success)
                {
                    _logger.LogWarning("Note(s) {NoteIds} not found for deletion for user: {UserEmail}", id, userEmail);
                    return NotFound(new { Message = $"Note(s) with ID(s) {id} not found" });
                }

                _logger.LogInformation("Successfully deleted note(s) {NoteIds} for user: {UserEmail}", id, userEmail);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for note deletion, NoteIds: {NoteIds}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for note deletion, NoteIds: {NoteIds}", id);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Note(s) {NoteIds} not found for deletion", id);
                return NotFound(new { Message = $"Note(s) with ID(s) {id} not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting note(s) {NoteIds}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while deleting the note(s)" });
            }
        }

    }
}
