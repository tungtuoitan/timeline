using Microsoft.AspNetCore.Mvc;
using SuperAppModels.Mos;
using SuperAppDataServices.Ins;
using SuperAppModels.DTOs;

namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotesController : Controller
    {
        private readonly INoteSe _noteSe;
        private readonly ILogger<NotesController> _logger;
        
        public NotesController(INoteSe noteSe, ILogger<NotesController> logger)
        {
            _noteSe = noteSe;
            _logger = logger;
        }

        [HttpGet("GetNotes")]
        public async Task<List<Note>> GetNotes(
            bool getAll = false,
            string? searchText = null,
            string? types = null,
            string? tags = null,
            string? createdBy = null)
        {
            var notes = await _noteSe.GetNotes(getAll, searchText, types, tags, createdBy);
            return notes;
        }

        [HttpPost("IuNote")]
        public async Task<IActionResult> IuNote([FromForm] Note note)
        {
            NotesResult res = await _noteSe.IuNote(note);
            if (res.Options.Success)
            {
                return Ok(res);
            }
            else
            {
                return BadRequest(new NotesResult { Notes = new List<Note>(), Options = res.Options });
            }
        }
    }
}