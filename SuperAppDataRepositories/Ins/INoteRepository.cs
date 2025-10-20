using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface INoteRepository
    {
        Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, List<int>? tagIds = null, int? createdByUserId = null);
        Task<Note?> GetNoteById(int noteId);
        Task<Note> CreateNoteAsync(Note note, List<int>? tagIds = null, int? createdByUserId = null);
        Task<Note> UpdateNoteAsync(Note note, List<int>? tagIds = null, int? createdByUserId = null);
        Task<bool> DeleteNoteAsync(string noteIds);
    }
}