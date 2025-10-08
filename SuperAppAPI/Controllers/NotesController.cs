using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperApp.Application.Features.Notes.Commands.CreateNote;
using SuperApp.Application.Features.Notes.Commands.DeleteNote;
using SuperApp.Application.Features.Notes.Commands.UpdateNote;
using SuperApp.Application.Features.Notes.Queries.GetNotes;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppAPI.Controllers
{
    //[Authorize]  // Temporarily commented out - Require authentication for all endpoints
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<NotesController> _logger;

        public NotesController(IMediator mediator, ILogger<NotesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get all notes with optional filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<NoteResponse>), 200)]
        public async Task<ActionResult<List<NoteResponse>>> GetNotes(
            [FromQuery] bool getAll = false,
            [FromQuery] string? searchText = null)
        {
            _logger.LogInformation("Getting notes. GetAll: {GetAll}, SearchText: {SearchText}", getAll, searchText);

            var query = new GetNotesQuery(getAll, searchText);
            var response = await _mediator.Send(query);

            return Ok(response);
        }

        /// <summary>
        /// Create a new note
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(NoteResponse), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<NoteResponse>> CreateNote([FromBody] CreateNoteRequest request)
        {
            _logger.LogInformation("Creating note: {Name}", request.Name);

            var command = new CreateNoteCommand(request);
            var response = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetNotes), new { }, response);
        }

        /// <summary>
        /// Update an existing note
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(NoteResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<NoteResponse>> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
        {
            if (id != request.NoteId)
            {
                return BadRequest(new { Message = "Note ID in URL does not match request body" });
            }

            _logger.LogInformation("Updating note: {NoteId}", request.NoteId);

            var command = new UpdateNoteCommand(request);
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        /// <summary>
        /// Delete a note
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteNote(int id)
        {
            _logger.LogInformation("Deleting note: {NoteId}", id);

            var command = new DeleteNoteCommand(id);
            var success = await _mediator.Send(command);

            if (success)
            {
                return NoContent();
            }
            
            return NotFound(new { Message = $"Note with ID {id} not found" });
        }
    }
}
