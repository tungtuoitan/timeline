using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.Mos;
using TLMos.DTOs;

namespace TLDataRes.Ins
{
    public interface INoteRe
    {
        Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null);
        Task<NotesResult> IuNote(Note note);
    }
}