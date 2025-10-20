using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperApp.Application.Features.Tags.Commands.CreateTag;
using SuperApp.Application.Features.Tags.Commands.DeleteTag;
using SuperApp.Application.Features.Tags.Commands.UpdateTag;
using SuperApp.Application.Features.Tags.Queries.GetTags;
using SuperApp.Application.Features.Tags.Queries.GetTagById;
using SuperApp.Application.Features.Tags.Queries.GetTagTree;
using SuperApp.Application.Features.Tags.Queries.GetWorkspaceTagTree;
using SuperApp.Application.Features.Notes.Queries.GetNotes;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing tag operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // TEMPORARY: Authorization disabled for development
    public class TagsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TagsController> _logger;

        public TagsController(IMediator mediator, ILogger<TagsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all tags with hierarchy information for the authenticated user
        /// </summary>
        /// <returns>List of tags with depth information ordered by hierarchy</returns>
        /// <response code="200">Tags retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<TagResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                // TEMPORARY: Using hardcoded userId while auth is disabled
                // In production, this should be extracted from the JWT token
                var userId = 1; // Hardcoded for development
                
                _logger.LogInformation("Retrieving tags for userId: {UserId}", userId);

                var query = new GetTagsQuery(userId);
                var response = await _mediator.Send(query);

                _logger.LogInformation("Successfully retrieved {TagCount} tags for userId: {UserId}",
                    response?.Count ?? 0, userId);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get tags");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for get tags");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving tags");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving tags" });
            }
        }

        /// <summary>
        /// Gets hierarchical tag tree for the authenticated user
        /// </summary>
        /// <param name="includeShared">Whether to include shared tags from other users</param>
        /// <returns>Hierarchical tag tree with usage statistics</returns>
        /// <response code="200">Tag tree retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("tree")]
        [ProducesResponseType(typeof(List<TagTreeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTagTree([FromQuery] bool includeShared = true)
        {
            try
            {
                // TEMPORARY: Using hardcoded userId while auth is disabled
                var userId = 1; // Hardcoded for development
                
                _logger.LogInformation("Retrieving tag tree for userId: {UserId}, includeShared: {IncludeShared}", 
                    userId, includeShared);

                var query = new GetTagTreeQuery(userId, includeShared);
                var response = await _mediator.Send(query);

                _logger.LogInformation("Successfully retrieved tag tree with {RootTagCount} root tags for userId: {UserId}",
                    response?.Count ?? 0, userId);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get tag tree");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for get tag tree");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving tag tree");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving tag tree" });
            }
        }

        /// <summary>
        /// Gets workspace information with its complete hierarchical tag tree
        /// </summary>
        /// <param name="workspaceId">Workspace ID</param>
        /// <returns>Workspace details with hierarchical tag tree structure</returns>
        /// <response code="200">Workspace with tag tree retrieved successfully</response>
        /// <response code="400">Invalid workspace ID</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="403">Access denied - no access to workspace</response>
        /// <response code="404">Workspace not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("workspace/{workspaceId:int}/tree")]
        [ProducesResponseType(typeof(WorkspaceWithTagTreeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceTagTree(int workspaceId)
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
                
                _logger.LogInformation("Retrieving workspace tag tree for workspaceId: {WorkspaceId}, userId: {UserId}", 
                    workspaceId, userId);

                var query = new GetWorkspaceTagTreeQuery(workspaceId, userId);
                var response = await _mediator.Send(query);

                _logger.LogInformation("Successfully retrieved workspace tag tree with {RootTagCount} root tags for workspaceId: {WorkspaceId}",
                    response?.Tags?.Count ?? 0, workspaceId);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get workspace tag tree: {WorkspaceId}", workspaceId);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for workspace tag tree: {WorkspaceId}", workspaceId);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving workspace tag tree for workspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving workspace tag tree" });
            }
        }

        /// <summary>
        /// Gets a specific tag by ID for the authenticated user
        /// </summary>
        /// <param name="id">Tag ID</param>
        /// <returns>Tag details if found and accessible</returns>
        /// <response code="200">Tag retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Tag not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTagById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid tag ID provided: {TagId}", id);
                    return BadRequest(new { Message = "Tag ID must be a positive integer" });
                }

                // TEMPORARY: Using hardcoded email while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                //if (string.IsNullOrEmpty(userEmail))
                //{
                //    _logger.LogWarning("Failed to extract user email from token");
                //    return Unauthorized(new { Message = "Invalid token claims" });
                //}

                _logger.LogInformation("Retrieving tag {TagId} for user: {UserEmail}", id, userEmail);

                var query = new GetTagByIdQuery(id);
                var response = await _mediator.Send(query);
                
                if (response == null)
                {
                    _logger.LogWarning("Tag {TagId} not found or not accessible for user: {UserEmail}", id, userEmail);
                    return NotFound(new { Message = $"Tag with ID {id} not found" });
                }

                _logger.LogInformation("Successfully retrieved tag {TagId} for user: {UserEmail}", id, userEmail);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get tag by ID: {TagId}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for tag {TagId}", id);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving tag {TagId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving the tag" });
            }
        }

        /// <summary>
        /// Creates a new tag for the authenticated user
        /// </summary>
        /// <param name="request">Tag creation data</param>
        /// <returns>Created tag with location header</returns>
        /// <response code="201">Tag created successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(TagResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for create tag request");
                    return BadRequest(ModelState);
                }

                // TEMPORARY: Using hardcoded userId while auth is disabled
                // In production, this should be extracted from the JWT token
                var userId = 1; // Hardcoded for development
                request.UserId = userId;

                _logger.LogInformation("Creating tag '{TagName}' for userId: {UserId}", request.Name, userId);

                var command = new CreateTagCommand(request);
                var response = await _mediator.Send(command);

                _logger.LogInformation("Tag created successfully with ID: {TagId} for userId: {UserId}", 
                    response.TagId, userId);

                return CreatedAtAction(nameof(GetTagById), new { id = response.TagId }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for tag creation");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for tag creation");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating tag");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while creating the tag" });
            }
        }

        /// <summary>
        /// Updates an existing tag for the authenticated user
        /// </summary>
        /// <param name="id">Tag ID to update</param>
        /// <param name="request">Tag data for update</param>
        /// <returns>Updated tag details</returns>
        /// <response code="200">Tag updated successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Tag not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid tag ID provided for update: {TagId}", id);
                    return BadRequest(new { Message = "Tag ID must be a positive integer" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for update tag request, TagId: {TagId}", id);
                    return BadRequest(ModelState);
                }

                // TEMPORARY: Using hardcoded email while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                //if (string.IsNullOrEmpty(userEmail))
                //{
                //    _logger.LogWarning("Failed to extract user email from token");
                //    return Unauthorized(new { Message = "Invalid token claims" });
                //}

                // Set the TagId from URL parameter
                request.TagId = id;

                _logger.LogInformation("Updating tag {TagId} for user: {UserEmail}", id, userEmail);

                var command = new UpdateTagCommand(request);
                var response = await _mediator.Send(command);

                if (response == null)
                {
                    _logger.LogWarning("Tag {TagId} not found or not accessible for user: {UserEmail}", id, userEmail);
                    return NotFound(new { Message = $"Tag with ID {id} not found" });
                }

                _logger.LogInformation("Successfully updated tag {TagId} for user: {UserEmail}", id, userEmail);
                return Ok(response);
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for tag update, TagId: {TagId}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for tag update, TagId: {TagId}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for tag update, TagId: {TagId}", id);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Tag {TagId} not found for update", id);
                return NotFound(new { Message = $"Tag with ID {id} not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating tag {TagId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while updating the tag" });
            }
        }

        /// <summary>
        /// Deletes a tag for the authenticated user
        /// </summary>
        /// <param name="id">Tag ID to delete</param>
        /// <returns>No content on successful deletion</returns>
        /// <response code="204">Tag deleted successfully</response>
        /// <response code="400">Invalid tag ID</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Tag not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid tag ID provided for deletion: {TagId}", id);
                    return BadRequest(new { Message = "Tag ID must be a positive integer" });
                }

                // TEMPORARY: Using hardcoded email while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                //if (string.IsNullOrEmpty(userEmail))
                //{
                //    _logger.LogWarning("Failed to extract user email from token");
                //    return Unauthorized(new { Message = "Invalid token claims" });
                //}

                _logger.LogInformation("Deleting tag {TagId} for user: {UserEmail}", id, userEmail);

                var command = new DeleteTagCommand(id);
                var success = await _mediator.Send(command);

                if (!success)
                {
                    _logger.LogWarning("Tag {TagId} not found for deletion for user: {UserEmail}", id, userEmail);
                    return NotFound(new { Message = $"Tag with ID {id} not found" });
                }

                _logger.LogInformation("Successfully deleted tag {TagId} for user: {UserEmail}", id, userEmail);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for tag deletion, TagId: {TagId}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for tag deletion, TagId: {TagId}", id);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Tag {TagId} not found for deletion", id);
                return NotFound(new { Message = $"Tag with ID {id} not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting tag {TagId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while deleting the tag" });
            }
        }

        /// <summary>
        /// Gets all tags associated with a specific note
        /// </summary>
        /// <param name="noteId">Note ID</param>
        /// <returns>List of tags associated with the note</returns>
        /// <response code="200">Tags retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Note not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("note/{noteId:int}")]
        [ProducesResponseType(typeof(List<TagResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTagsByNoteId(int noteId)
        {
            try
            {
                if (noteId <= 0)
                {
                    _logger.LogWarning("Invalid note ID provided: {NoteId}", noteId);
                    return BadRequest(new { Message = "Note ID must be a positive integer" });
                }

                // TEMPORARY: Using hardcoded email while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                //if (string.IsNullOrEmpty(userEmail))
                //{
                //    _logger.LogWarning("Failed to extract user email from token");
                //    return Unauthorized(new { Message = "Invalid token claims" });
                //}

                _logger.LogInformation("Retrieving tags for note {NoteId} for user: {UserEmail}", noteId, userEmail);

                // This would require a new query/handler for getting tags by note ID
                // For now, we can get the note and return its tags
                var noteQuery = new GetNotesQuery(false, null, null);
                var notes = await _mediator.Send(noteQuery);
                var note = notes.FirstOrDefault(n => n.NoteId == noteId);

                if (note == null)
                {
                    _logger.LogWarning("Note {NoteId} not found for user: {UserEmail}", noteId, userEmail);
                    return NotFound(new { Message = $"Note with ID {noteId} not found" });
                }

                _logger.LogInformation("Successfully retrieved {TagCount} tags for note {NoteId}", 
                    note.Tags.Count, noteId);

                return Ok(note.Tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving tags for note {NoteId}", noteId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while retrieving tags for the note" });
            }
        }

        /// <summary>
        /// Gets all notes associated with a specific tag
        /// </summary>
        /// <param name="tagId">Tag ID</param>
        /// <returns>List of notes associated with the tag</returns>
        /// <response code="200">Notes retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Tag not found or not accessible</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{tagId:int}/notes")]
        [ProducesResponseType(typeof(List<NoteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNotesByTagId(int tagId)
        {
            try
            {
                if (tagId <= 0)
                {
                    _logger.LogWarning("Invalid tag ID provided: {TagId}", tagId);
                    return BadRequest(new { Message = "Tag ID must be a positive integer" });
                }

                // TEMPORARY: Using hardcoded email while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                //if (string.IsNullOrEmpty(userEmail))
                //{
                //    _logger.LogWarning("Failed to extract user email from token");
                //    return Unauthorized(new { Message = "Invalid token claims" });
                //}

                _logger.LogInformation("Retrieving notes for tag {TagId} for user: {UserEmail}", tagId, userEmail);

                // Check if tag exists
                var tagQuery = new GetTagByIdQuery(tagId);
                var tag = await _mediator.Send(tagQuery);

                if (tag == null)
                {
                    _logger.LogWarning("Tag {TagId} not found for user: {UserEmail}", tagId, userEmail);
                    return NotFound(new { Message = $"Tag with ID {tagId} not found" });
                }

                // Get notes filtered by this tag
                var notesQuery = new GetNotesQuery(false, null, new List<int> { tagId });
                var notes = await _mediator.Send(notesQuery);

                _logger.LogInformation("Successfully retrieved {NoteCount} notes for tag {TagId}", 
                    notes.Count, tagId);

                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving notes for tag {TagId}", tagId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while retrieving notes for the tag" });
            }
        }
    }
}