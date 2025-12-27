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
        public async Task<ResultOptions> GetNotesAsync(NoteFilterOptions filterOptions)
        {
            try
            {
                _logger.LogInformation("Getting notes for UserId: {UserId}, SearchText: {SearchText}, TagIds: {TagIds}, StatusCodes: {StatusCodes}, DeletedAt: {DeletedAt}, CreatedFrom: {CreatedFrom}, CreatedTo: {CreatedTo}",
                    filterOptions.UserId, filterOptions.SearchText,
                    filterOptions.TagIds != null ? string.Join(",", filterOptions.TagIds) : "null",
                    filterOptions.StatusCodes != null ? string.Join(",", filterOptions.StatusCodes) : "null",
                    filterOptions.DeletedAt, filterOptions.CreatedFrom, filterOptions.CreatedTo);

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
        /// Batch upsert multiple notes (create or update)
        /// For single note operations, pass a list with 1 element
        /// </summary>
        public async Task<ResultOptions> UpsertNotesAsync(List<UpsertNoteRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    _logger.LogWarning("Empty note batch upsert request");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No notes provided for batch upsert",
                        Status = 400
                    };
                }

                _logger.LogInformation("Batch upserting {Count} notes with transaction (all-or-nothing)", requests.Count);

                // Prepare notes for batch upsert
                var noteRequests = new List<(Note note, List<int>? tagIds)>();

                foreach (var request in requests)
                {
                    // Validation: can't soft delete a new note
                    if (request.DeletedAt.HasValue && request.Id == 0)
                    {
                        _logger.LogError("Cannot set deletedAt on a new note");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "Cannot set deletedAt on a new note. Use ID > 0 for soft delete/restore. All changes rolled back.",
                            Status = 400
                        };
                    }

                    // Validate UserId
                    if (!request.UserId.HasValue || request.UserId.Value <= 0)
                    {
                        _logger.LogError("UserId is required for all notes");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "UserId is required for all notes. All changes rolled back.",
                            Status = 400
                        };
                    }

                    var note = _mapper.Map<Note>(request);
                    note.Id = request.Id;
                    note.DeletedAt = request.DeletedAt;  // Map deletedAt for soft delete/restore
                    note.UserId = request.UserId.Value;

                    noteRequests.Add((note, request.TagIds));
                }

                // Execute batch upsert with transaction (all-or-nothing)
                var result = await _noteRepository.UpsertNotesAsync(noteRequests);

                if (!result.Success)
                {
                    _logger.LogError("Batch upsert failed: {Message}", result.Message);
                    return result;
                }

                // Map Note entities to NoteDTOs
                var notes = result.Data?.Cast<Note>().ToList() ?? new List<Note>();
                var noteDTOs = _mapper.Map<List<NoteDTO>>(notes);

                _logger.LogInformation("Batch upsert transaction completed successfully: {Count} notes upserted",
                    noteDTOs.Count);

                return new ResultOptions
                {
                    Success = true,
                    Message = $"Successfully upserted {noteDTOs.Count} notes in batch transaction",
                    Data = noteDTOs.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during batch upsert");
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
        public async Task<ResultOptions> DeleteNotesAsync(List<int> noteIds)
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

                _logger.LogInformation("Deleting notes with IDs: {NoteIds})",
                    string.Join(",", noteIds));

                // Repository now returns ResultOptions
                var result = await _noteRepository.DeleteNotesAsync(noteIds);

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

    }
}
