using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Notes.Commands.DeleteNote;
using SuperAppDataRepositories.Ins;

namespace SuperApp.Application.Features.Notes.Commands.DeleteNote
{
    public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand, bool>
    {
        private readonly INoteRepository _noteRepository;
        private readonly ILogger<DeleteNoteCommandHandler> _logger;

        public DeleteNoteCommandHandler(
            INoteRepository noteRepository,
            ILogger<DeleteNoteCommandHandler> logger)
        {
            _noteRepository = noteRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting note with ID: {NoteId}", request.NoteId);

                await _noteRepository.DNote(request.NoteId);
                
                _logger.LogInformation("Successfully deleted note with ID: {NoteId}", request.NoteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting note with ID: {NoteId}", request.NoteId);
                return false;
            }
        }
    }
}