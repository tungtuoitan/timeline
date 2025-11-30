using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface INoteRepository
    {
        Task<List<Note>> GetNotesAsync(string userEmail, bool getAll, string? searchText);
        Task<List<Note>> GetNotes(bool getAll, string searchText, List<int>? tagIds);
        Task<Note?> GetNoteByIdAsync(int noteId);
        Task<Note?> GetNoteById(int noteId);
        Task<Note> CreateNoteAsync(Note note);
        Task<Note> CreateNoteAsync(Note note, List<int>? tagIds, int? parentId);
        Task<bool> UpdateNoteAsync(Note note);
        Task<Note> UpdateNoteAsync(Note note, List<int>? tagIds, int? parentId);
        Task<bool> DeleteNoteAsync(int noteId);
        Task<bool> DeleteNoteAsync(string noteId);
    }
}
