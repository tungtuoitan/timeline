using SuperAppModels.DTOs.Responses;

namespace SuperAppModels.DTOs
{
    public class NotesResult
    {
        public List<NoteResponse> Notes { get; set; } = new List<NoteResponse>();
        public ResultOptions Options { get; set; } = new ResultOptions();
    }
}