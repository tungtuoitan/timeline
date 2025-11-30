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
        Task<bool> DeleteNoteAsync(int noteId);
    }
}
