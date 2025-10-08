using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperAppModels.Mos;
using SuperAppModels.DTOs;

namespace SuperAppDataRepositories.Ins
{
    public interface INoteRepository
    {
        Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null);
        Task<NotesResult> IuNote(Note note);
        Task<bool> DNote(int noteId);
    }
}