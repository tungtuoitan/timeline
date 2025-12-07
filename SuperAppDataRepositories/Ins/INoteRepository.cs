using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface INoteRepository
    {
        Task<ResultOptions> GetNotesAsync(NoteFilterOptions filterOptions);
        Task<ResultOptions> GetNoteById(int noteId);
        Task<ResultOptions> CreateNoteAsync(Note note, List<int>? tagIds, int? parentId);
        Task<ResultOptions> UpdateNoteAsync(Note note, List<int>? tagIds, int? parentId);
        Task<ResultOptions> DeleteNotesBatchAsync(List<int> noteIds, bool isHardDelete = false);
        Task<ResultOptions> UndoDeleteNotesBatchAsync(List<int> noteIds);
    }
}
