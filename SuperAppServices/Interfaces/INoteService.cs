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
        /// <param name="filterOptions">Filter options including userId, searchText, tagIds, status, dates, etc.</param>
        /// <returns>ResultOptions containing list of notes</returns>
        Task<ResultOptions> GetNotesAsync(NoteFilterOptions filterOptions);

        /// <summary>
        /// Get note by ID
        /// </summary>
        /// <param name="noteId">Note ID</param>
        /// <returns>ResultOptions containing note details</returns>
        Task<ResultOptions> GetNoteByIdAsync(int noteId);

        /// <summary>
        /// Batch create or update multiple notes (upsert)
        /// For single note operations, pass a list with 1 element
        /// </summary>
        /// <param name="requests">List of note upsert requests</param>
        /// <returns>ResultOptions containing batch operation results</returns>
        Task<ResultOptions> UpsertNotesAsync(List<UpsertNoteRequest> requests);

        /// <summary>
        /// Delete notes by IDs
        /// </summary>
        /// <param name="noteIds">List of note IDs to delete</param>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> DeleteNotesAsync(List<int> noteIds);

    }
}
