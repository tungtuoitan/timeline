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
        public async Task<ResultOptions> GetNotesAsync(int userId, string? searchText, List<int>? tagIds)
        {
            try
            {
                _logger.LogInformation("Getting notes for UserId: {UserId}, SearchText: {SearchText}, TagIds: {TagIds}",
                    userId, searchText, tagIds != null ? string.Join(",", tagIds) : "null");

                var filterOptions = new NoteFilterOptions
                {
                    UserId = userId,  // ✅ Filter by userId
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
        /// Batch upsert multiple notes (create or update)
        /// For single note operations, pass a list with 1 element
        /// </summary>
        public async Task<ResultOptions> UpsertNotesBatchAsync(List<UpsertNoteRequest> requests)
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

                _logger.LogInformation("Batch upserting {Count} notes", requests.Count);

                var successCount = 0;
                var failCount = 0;
                var errors = new List<string>();
                var results = new List<NoteDTO>();

                foreach (var request in requests)
                {
                    try
                    {
                        _logger.LogInformation("Processing note with ID: {NoteId}, Name: '{Name}', TagIds: [{TagIds}]",
                            request.Id,
                            request.Name,
                            request.TagIds != null ? string.Join(",", request.TagIds) : "null");

                        var note = _mapper.Map<Note>(request);
                        note.Id = request.Id;
                        note.DeletedAt = request.DeletedAt;  // Map deletedAt for soft delete/restore

                        // Validation: can't soft delete a new note
                        if (request.DeletedAt.HasValue && request.Id == 0)
                        {
                            throw new ArgumentException("Cannot set deletedAt on a new note. Use ID > 0 for soft delete/restore.");
                        }

                        // Set UserId from request (populated by controller from JWT claims)
                        if (request.UserId.HasValue && request.UserId.Value > 0)
                        {
                            note.UserId = request.UserId.Value;
                        }
                        else
                        {
                            throw new ArgumentException("UserId is required");
                        }

                        // Upsert note (create or update)
                        var result = await _noteRepository.UpsertNoteAsync(note, request.TagIds, null);

                        if (!result.Success)
                        {
                            errors.Add($"Note ID {request.Id}: {result.Message}");
                            failCount++;
                            continue;
                        }

                        // Map Note entity to NoteDTO
                        var resultNote = result.Object as Note;
                        if (resultNote == null)
                        {
                            errors.Add($"Note ID {request.Id}: Failed to process note");
                            failCount++;
                            continue;
                        }

                        var response = _mapper.Map<NoteDTO>(resultNote);
                        results.Add(response);
                        successCount++;

                        _logger.LogInformation("Successfully processed note with ID: {NoteId}, final TagCount: {TagCount}",
                            resultNote.Id, response.Tags?.Count ?? 0);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "Invalid argument while processing note with ID: {NoteId}", request.Id);
                        errors.Add($"Note ID {request.Id}: {ex.Message}");
                        failCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error occurred while processing note with ID: {NoteId}", request.Id);
                        errors.Add($"Note ID {request.Id}: {ex.Message}");
                        failCount++;
                    }
                }

                var message = successCount > 0
                    ? $"Successfully upserted {successCount}/{requests.Count} notes"
                    : "Failed to upsert all notes";

                if (failCount > 0)
                {
                    message += $". {failCount} failed.";
                }

                _logger.LogInformation("Batch upsert completed: {SuccessCount} succeeded, {FailCount} failed",
                    successCount, failCount);

                return new ResultOptions
                {
                    Success = successCount > 0,
                    Message = message,
                    Object = new
                    {
                        SuccessCount = successCount,
                        FailCount = failCount,
                        Errors = errors,
                        Notes = results
                    },
                    Status = successCount > 0 ? 200 : 400
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
                var result = await _noteRepository.DeleteNotesBatchAsync(noteIds);

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
