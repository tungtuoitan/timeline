using TLMos.Mos;
using TLMos.DTOs;

namespace TLDataSes.Ins
{
    public interface INoteSe
    {
        Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null);
        Task<NotesResult> IuNote(Note note);
    }
}