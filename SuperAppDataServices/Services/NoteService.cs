using SuperAppModels.Mos;
using Microsoft.AspNetCore.Http;
using SuperAppDataServices.Ins;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;

namespace SuperAppDataServices.Services
{
    public class NoteService : INoteSe
    {
        private readonly INoteRepository _noteRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public NoteService(INoteRepository noteRepo, IHttpContextAccessor httpContextAccessor)
        {
            _noteRepository = noteRepo;
            _httpContextAccessor = httpContextAccessor;
        }
        
        public async Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null)
        {
            List<Note> notes = await _noteRepository.GetNotes(getAll, searchText, types, tags, createdBy);
            return notes;
        }

        public async Task<NotesResult> IuNote(Note note)
        {
            return await _noteRepository.IuNote(note);
        }
    }
}