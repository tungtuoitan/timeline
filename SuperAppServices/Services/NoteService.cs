using AutoMapper;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
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
        public async Task<List<NoteResponse>> GetNotesAsync(bool getAll, string? searchText, List<int>? tagIds)
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

                var notes = await _noteRepository.GetNotesAsync(filterOptions);
                var response = _mapper.Map<List<NoteResponse>>(notes);

                _logger.LogInformation("Successfully retrieved {Count} notes", response.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting notes");
                throw;
            }
        }

        /// <summary>
        /// Get note by ID
        /// </summary>
        public async Task<NoteResponse> GetNoteByIdAsync(int noteId)
        {
            try
            {
                _logger.LogInformation("Getting note with ID: {NoteId}", noteId);

                var note = await _noteRepository.GetNoteById(noteId);

                if (note != null)
                {
                    var response = _mapper.Map<NoteResponse>(note);
                    _logger.LogInformation("Successfully retrieved note with ID: {NoteId}", noteId);
                    return response;
                }

                _logger.LogWarning("Note not found with ID: {NoteId}", noteId);
                throw new KeyNotFoundException($"Note not found with ID: {noteId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting note with ID: {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Create or update note (upsert)
        /// </summary>
        public async Task<NoteResponse> UpsertNoteAsync(UpsertNoteRequest request)
        {
            try
            {
                _logger.LogInformation("Processing note with ID: {NoteId}, Name: '{Name}', TagIds: [{TagIds}]",
                    request.NoteId,
                    request.Name,
                    request.TagIds != null ? string.Join(",", request.TagIds) : "null");

                var note = _mapper.Map<Note>(request);
                note.Id = request.NoteId;

                Note resultNote;

                if (request.NoteId == 0)
                {
                    // TEMPORARY: Set UserId = 1 for development (auth disabled)
                    // TODO: Get userId from authenticated user when auth is enabled
                    note.UserId = 1;

                    _logger.LogInformation("Creating new note with name: '{Name}', UserId: {UserId}",
                        request.Name, note.UserId);
                    resultNote = await _noteRepository.CreateNoteAsync(note, request.TagIds, null);
                }
                else
                {
                    _logger.LogInformation("Updating existing note with ID: {NoteId}, Name: '{Name}'",
                        request.NoteId, request.Name);
                    resultNote = await _noteRepository.UpdateNoteAsync(note, request.TagIds, null);
                }

                var response = _mapper.Map<NoteResponse>(resultNote);

                _logger.LogInformation("Successfully processed note with ID: {NoteId}, final TagCount: {TagCount}",
                    resultNote.Id, response.Tags?.Count ?? 0);
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while processing note with ID: {NoteId}", request.NoteId);
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while processing note with ID: {NoteId}", request.NoteId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while processing note with ID: {NoteId}", request.NoteId);
                throw;
            }
        }

        /// <summary>
        /// Delete notes by IDs
        /// </summary>
        public async Task<bool> DeleteNotesAsync(List<int> noteIds)
        {
            try
            {
                _logger.LogInformation("Deleting notes with IDs: {NoteIds}", string.Join(",", noteIds));

                // Delete each note individually
                foreach (var noteId in noteIds)
                {
                    await _noteRepository.DeleteNoteAsync(noteId);
                }

                _logger.LogInformation("Successfully deleted notes with IDs: {NoteIds}", string.Join(",", noteIds));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting notes with IDs: {NoteIds}", string.Join(",", noteIds));
                throw;
            }
        }
    }
}
