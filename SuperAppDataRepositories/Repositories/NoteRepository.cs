using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class NoteRepository : INoteRepository
    {
        public Task<List<Note>> GetNotesAsync(string userEmail, bool getAll, string? searchText)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<List<Note>> GetNotes(bool getAll, string searchText, List<int>? tagIds)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<Note?> GetNoteByIdAsync(int noteId)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<Note?> GetNoteById(int noteId)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<Note> CreateNoteAsync(Note note)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<Note> CreateNoteAsync(Note note, List<int>? tagIds, int? parentId)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<bool> UpdateNoteAsync(Note note)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<Note> UpdateNoteAsync(Note note, List<int>? tagIds, int? parentId)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<bool> DeleteNoteAsync(int noteId)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<bool> DeleteNoteAsync(string noteId)
        {
            throw new NotImplementedException("NoteRepository chưa được implement - cần migrate từ code cũ");
        }
    }
}
