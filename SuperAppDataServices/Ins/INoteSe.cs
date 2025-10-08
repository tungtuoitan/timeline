using SuperAppModels.Mos;
using SuperAppModels.DTOs;

namespace SuperAppDataServices.Ins
{
    public interface INoteSe
    {
        Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null);
        Task<NotesResult> IuNote(Note note);
    }
}