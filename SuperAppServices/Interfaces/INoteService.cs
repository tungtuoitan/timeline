using SuperAppModels.DTOs;
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
        /// <param name="userId">User ID to filter notes</param>
        /// <param name="getAll">Get all notes or only active</param>
        /// <param name="searchText">Search text filter</param>
        /// <param name="tagIds">Filter by tag IDs</param>
        /// <returns>ResultOptions containing list of notes</returns>
        Task<ResultOptions> GetNotesAsync(int userId, bool getAll, string? searchText, List<int>? tagIds);

        /// <summary>
        /// Get note by ID
        /// </summary>
        /// <param name="noteId">Note ID</param>
        /// <returns>ResultOptions containing note details</returns>
        Task<ResultOptions> GetNoteByIdAsync(int noteId);

        /// <summary>
        /// Create or update note (upsert)
        /// </summary>
        /// <param name="request">Note upsert request</param>
        /// <returns>ResultOptions containing created or updated note</returns>
        Task<ResultOptions> UpsertNoteAsync(UpsertNoteRequest request);

        /// <summary>
        /// Delete notes by IDs
        /// </summary>
        /// <param name="noteIds">List of note IDs to delete</param>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> DeleteNotesAsync(List<int> noteIds, bool isHardDelete = false);

        /// <summary>
        /// Restore deleted notes by setting deleted_at to null
        /// </summary>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> UndoDeleteNotesAsync(List<int> noteIds);
    }
}
