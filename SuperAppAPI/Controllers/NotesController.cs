using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperApp.Application.Features.Notes.Commands.CreateNote;
using SuperApp.Application.Features.Notes.Commands.DeleteNote;
using SuperApp.Application.Features.Notes.Commands.UpdateNote;
using SuperApp.Application.Features.Notes.Queries.GetNotes;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing notes operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Restore authorization for all endpoints - security critical!
    public class NotesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<NotesController> _logger;

        public NotesController(IMediator mediator, ILogger<NotesController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all notes with optional filtering for the authenticated user
        /// </summary>
        /// <param name="getAll">Get all notes flag (admin only)</param>
        /// <param name="searchText">Optional search text filter</param>
        /// <returns>List of notes matching the criteria</returns>
        /// <response code="200">Notes retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(SuperAppModels.DTOs.NotesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNotes([FromQuery] bool getAll = false, [FromQuery] string? searchText = null)
        {
            try
            {
                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Retrieving notes for user: {UserEmail}, GetAll: {GetAll}, SearchText: {SearchText}",
                    userEmail, getAll, searchText);

                var query = new GetNotesQuery(getAll, searchText);
                var response = await _mediator.Send(query);

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
        /// Creates a new note for the authenticated user
        /// </summary>
        /// <param name="request">Note creation data</param>
        /// <returns>Created note with location header</returns>
        /// <response code="201">Note created successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for create note request");
                    return BadRequest(ModelState);
                }

                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                // Set the CreatedBy from the authenticated user
                request.CreatedBy = userEmail;

                _logger.LogInformation("Creating note '{NoteName}' for user: {UserEmail}", request.Name, userEmail);

                var command = new CreateNoteCommand(request);
                var response = await _mediator.Send(command);

                _logger.LogInformation("Note created successfully with ID: {NoteId} for user: {UserEmail}", 
                    response.NoteId, userEmail);

                return CreatedAtAction(nameof(GetNoteById), new { id = response.NoteId }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for note creation");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for note creation");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating note");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while creating the note" });
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

                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Retrieving note {NoteId} for user: {UserEmail}", id, userEmail);

                var query = new GetNotesQuery(false, null);
                var response = await _mediator.Send(query);
                
                var note = response?.FirstOrDefault();
                if (note == null)
                {
                    _logger.LogWarning("Note {NoteId} not found or not accessible for user: {UserEmail}", id, userEmail);
                    return NotFound(new { Message = $"Note with ID {id} not found" });
                }

                _logger.LogInformation("Successfully retrieved note {NoteId} for user: {UserEmail}", id, userEmail);
                return Ok(note);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get note by ID: {NoteId}", id);
                return BadRequest(new { Message = ex.Message });
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
        /// Updates an existing note for the authenticated user
        /// </summary>
        /// <param name="id">Note ID to update</param>
        /// <param name="request">Note update data</param>
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
        public async Task<IActionResult> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid note ID provided for update: {NoteId}", id);
                    return BadRequest(new { Message = "Note ID must be a positive integer" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for update note request, NoteId: {NoteId}", id);
                    return BadRequest(ModelState);
                }

                // Ensure the ID in the URL matches the request if NoteId is provided in request
                if (request.NoteId !=0 && request.NoteId != id)
                {
                    _logger.LogWarning("Note ID mismatch: URL ID {UrlId}, Request ID {RequestId}", id, request.NoteId);
                    return BadRequest(new { Message = "Note ID in URL does not match request body" });
                }

                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Updating note {NoteId} for user: {UserEmail}", id, userEmail);

                // Set the NoteId in request to ensure consistency
                if (request.NoteId == 0)
                {
                    request.NoteId = id;
                }

                var command = new UpdateNoteCommand(request);
                var response = await _mediator.Send(command);

                if (response == null)
                {
                    _logger.LogWarning("Note {NoteId} not found or not accessible for user: {UserEmail}", id, userEmail);
                    return NotFound(new { Message = $"Note with ID {id} not found" });
                }

                _logger.LogInformation("Successfully updated note {NoteId} for user: {UserEmail}", id, userEmail);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for note update, NoteId: {NoteId}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for note update, NoteId: {NoteId}", id);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating note {NoteId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while updating the note" });
            }
        }

        /// <summary>
        /// Deletes a note for the authenticated user
        /// </summary>
        /// <param name="id">Note ID to delete</param>
        /// <returns>No content on successful deletion</returns>
        /// <response code="204">Note deleted successfully</response>
        /// <response code="400">Invalid note ID</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Note not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteNote(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid note ID provided for deletion: {NoteId}", id);
                    return BadRequest(new { Message = "Note ID must be a positive integer" });
                }

                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Deleting note {NoteId} for user: {UserEmail}", id, userEmail);

                var command = new DeleteNoteCommand(id);
                var success = await _mediator.Send(command);

                if (!success)
                {
                    _logger.LogWarning("Note {NoteId} not found for deletion for user: {UserEmail}", id, userEmail);
                    return NotFound(new { Message = $"Note with ID {id} not found" });
                }

                _logger.LogInformation("Successfully deleted note {NoteId} for user: {UserEmail}", id, userEmail);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for note deletion, NoteId: {NoteId}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for note deletion, NoteId: {NoteId}", id);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Note {NoteId} not found for deletion", id);
                return NotFound(new { Message = $"Note with ID {id} not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting note {NoteId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while deleting the note" });
            }
        }
    }
}
