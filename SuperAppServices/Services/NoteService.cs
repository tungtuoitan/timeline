using AutoMapper;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for note operations
    /// </summary>
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<NoteService> _logger;

        public NoteService(
            INoteRepository noteRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<NoteService> logger)
        {
            _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all notes with optional filters
        /// </summary>
        public async Task<ResultOptions> GetNotesAsync(bool getAll, string? searchText, List<int>? tagIds)
        {
            try
            {
                _logger.LogInformation("Getting notes with GetAll: {GetAll}, SearchText: {SearchText}, TagIds: {TagIds}",
                    getAll, searchText, tagIds != null ? string.Join(",", tagIds) : "null");

                var filterOptions = new NoteFilterOptions
                {
                    GetAll = getAll,
                    SearchText = searchText,
                    TagIds = tagIds
                };

                // Repository now returns ResultOptions
                var result = await _noteRepository.GetNotesAsync(filterOptions);
                
                if (!result.Success)
                {
                    return result; // Return repository error as-is
                }

                // Map Note entities to NoteDTO
                var notes = result.Data?.Cast<Note>().ToList() ?? new List<Note>();
                var response = _mapper.Map<List<NoteDTO>>(notes);

                _logger.LogInformation("Successfully retrieved {Count} notes", response.Count);
                
                return new ResultOptions
                {
                    Success = true,
                    Message = "Notes retrieved successfully",
                    Data = response.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting notes");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Get note by ID
        /// </summary>
        public async Task<ResultOptions> GetNoteByIdAsync(int noteId)
        {
            try
            {
                _logger.LogInformation("Getting note with ID: {NoteId}", noteId);

                // Repository now returns ResultOptions
                var result = await _noteRepository.GetNoteById(noteId);

                if (!result.Success)
                {
                    return result; // Return repository error as-is
                }

                // Map Note entity to NoteDTO
                var note = result.Object as Note;
                if (note != null)
                {
                    var response = _mapper.Map<NoteDTO>(note);
                    _logger.LogInformation("Successfully retrieved note with ID: {NoteId}", noteId);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Note retrieved successfully",
                        Object = response,
                        Status = 200
                    };
                }

                _logger.LogWarning("Note not found with ID: {NoteId}", noteId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Note not found with ID: {noteId}",
                    Status = 404
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting note with ID: {NoteId}", noteId);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Create or update note (upsert)
        /// </summary>
        public async Task<ResultOptions> UpsertNoteAsync(UpsertNoteRequest request)
        {
            try
            {
                _logger.LogInformation("Processing note with ID: {NoteId}, Name: '{Name}', TagIds: [{TagIds}]",
                    request.Id,
                    request.Name,
                    request.TagIds != null ? string.Join(",", request.TagIds) : "null");

                var note = _mapper.Map<Note>(request);
                note.Id = request.Id;

                ResultOptions result;
                bool isCreate = request.Id == 0;

                if (isCreate)
                {
                    // TEMPORARY: Set UserId = 1 for development (auth disabled)
                    // TODO: Get userId from authenticated user when auth is enabled
                    note.UserId = 1;

                    _logger.LogInformation("Creating new note with name: '{Name}', UserId: {UserId}",
                        request.Name, note.UserId);
                    result = await _noteRepository.CreateNoteAsync(note, request.TagIds, null);
                }
                else
                {
                    _logger.LogInformation("Updating existing note with ID: {NoteId}, Name: '{Name}'",
                        request.Id, request.Name);
                    result = await _noteRepository.UpdateNoteAsync(note, request.TagIds, null);
                }

                if (!result.Success)
                {
                    return result; // Return repository error as-is
                }

                // Map Note entity to NoteDTO
                var resultNote = result.Object as Note;
                if (resultNote == null)
                {
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Failed to process note",
                        Status = 500
                    };
                }

                var response = _mapper.Map<NoteDTO>(resultNote);

                _logger.LogInformation("Successfully processed note with ID: {NoteId}, final TagCount: {TagCount}",
                    resultNote.Id, response.Tags?.Count ?? 0);
                
                return new ResultOptions
                {
                    Success = true,
                    Message = isCreate ? "Note created successfully" : "Note updated successfully",
                    Object = response,
                    Status = isCreate ? 201 : 200
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while processing note with ID: {NoteId}", request.Id);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 400
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while processing note with ID: {NoteId}", request.Id);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 400
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while processing note with ID: {NoteId}", request.Id);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Delete notes by IDs (soft or hard delete)
        /// </summary>
        public async Task<ResultOptions> DeleteNotesAsync(List<int> noteIds, bool isHardDelete = false)
        {
            try
            {
                if (noteIds == null || !noteIds.Any())
                {
                    _logger.LogWarning("Empty note IDs provided for deletion");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No note IDs provided",
                        Status = 400
                    };
                }

                _logger.LogInformation("Deleting notes with IDs: {NoteIds} (HardDelete: {IsHardDelete})", 
                    string.Join(",", noteIds), isHardDelete);

                // Repository now returns ResultOptions
                var result = await _noteRepository.DeleteNotesBatchAsync(noteIds, isHardDelete);

                _logger.LogInformation("Delete operation result: Success={Success}, Message={Message}", result.Success, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting notes with IDs: {NoteIds}", string.Join(",", noteIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Restore deleted notes by setting deleted_at to null
        /// </summary>
        public async Task<ResultOptions> UndoDeleteNotesAsync(List<int> noteIds)
        {
            try
            {
                if (noteIds == null || !noteIds.Any())
                {
                    _logger.LogWarning("Empty note IDs provided for undo delete");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No note IDs provided",
                        Status = 400
                    };
                }

                _logger.LogInformation("Restoring notes with IDs: {NoteIds}", string.Join(",", noteIds));

                // Repository now returns ResultOptions
                var result = await _noteRepository.UndoDeleteNotesBatchAsync(noteIds);

                _logger.LogInformation("Restore operation result: Success={Success}, Message={Message}", result.Success, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while restoring notes with IDs: {NoteIds}", string.Join(",", noteIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }
    }
}
