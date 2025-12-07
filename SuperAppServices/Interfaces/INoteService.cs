using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for note operations
    /// </summary>
    public interface INoteService
    {
        /// <summary>
        /// Get all notes with optional filters
        /// </summary>
        /// <param name="getAll">Get all notes or only active</param>
        /// <param name="searchText">Search text filter</param>
        /// <param name="tagIds">Filter by tag IDs</param>
        /// <returns>List of notes</returns>
        Task<List<NoteResponse>> GetNotesAsync(bool getAll, string? searchText, List<int>? tagIds);

        /// <summary>
        /// Get note by ID
        /// </summary>
        /// <param name="noteId">Note ID</param>
        /// <returns>Note details</returns>
        Task<NoteResponse> GetNoteByIdAsync(int noteId);

        /// <summary>
        /// Create or update note (upsert)
        /// </summary>
        /// <param name="request">Note upsert request</param>
        /// <returns>Created or updated note</returns>
        Task<NoteResponse> UpsertNoteAsync(UpsertNoteRequest request);

        /// <summary>
        /// Delete notes by IDs
        /// </summary>
        /// <param name="noteIds">List of note IDs to delete</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteNotesAsync(List<int> noteIds);

        /// <summary>
        /// Restore deleted notes by setting deleted_at to null
        /// </summary>
        Task<bool> UndoDeleteNotesAsync(List<int> noteIds);
    }
}
