using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface INoteRepository
    {
        Task<List<Note>> GetNotesAsync(NoteFilterOptions filterOptions);
        Task<Note?> GetNoteById(int noteId);
        Task<Note> CreateNoteAsync(Note note, List<int>? tagIds, int? parentId);
        Task<Note> UpdateNoteAsync(Note note, List<int>? tagIds, int? parentId);
        Task<int> DeleteNotesBatchAsync(List<int> noteIds, bool isHardDelete = false);
        Task<int> UndoDeleteNotesBatchAsync(List<int> noteIds);
    }
}
