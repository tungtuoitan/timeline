using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface INoteRepository
    {
        Task<ResultOptions> GetNotesAsync(NoteFilterOptions filterOptions);
        Task<ResultOptions> GetNoteById(int noteId);
        Task<ResultOptions> UpsertNotesAsync(List<(Note note, List<int>? tagIds)> noteRequests);
        Task<ResultOptions> DeleteNotesAsync(List<int> noteIds);
    }
}
