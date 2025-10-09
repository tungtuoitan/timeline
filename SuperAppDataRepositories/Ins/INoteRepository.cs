using SuperAppModels.Models;
using SuperAppModels.DTOs.Responses;

namespace SuperAppDataRepositories.Ins
{
    public interface INoteRepository
    {
        Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null);
        Task<Note?> GetNoteById(int noteId);
        Task<Note> CreateNoteAsync(Note note);
        Task<Note> UpdateNoteAsync(Note note);
        Task<bool> DeleteNoteAsync(int noteId);
        
        // Legacy methods for backward compatibility
        Task<NotesResult> IuNote(Note note);
        Task<bool> DNote(int noteId);
    }
}