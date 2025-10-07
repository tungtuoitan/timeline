using TLMos.Mos;
using Microsoft.AspNetCore.Http;
using TLDataSes.Ins;
using TLDataRes.Ins;
using TLMos.DTOs;

namespace TLDataSes.Ses
{
    public class NoteSe : INoteSe
    {
        private readonly INoteRe _noteRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public NoteSe(INoteRe noteRepo, IHttpContextAccessor httpContextAccessor)
        {
            _noteRepo = noteRepo;
            _httpContextAccessor = httpContextAccessor;
        }
        
        public async Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null)
        {
            List<Note> notes = await _noteRepo.GetNotes(getAll, searchText, types, tags, createdBy);
            return notes;
        }

        public async Task<NotesResult> IuNote(Note note)
        {
            return await _noteRepo.IuNote(note);
        }
    }
}