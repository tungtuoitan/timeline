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

                var result = await _noteRepository.DNote(request.NoteId);
                
                if (result)
                {
                    _logger.LogInformation("Successfully deleted note with ID: {NoteId}", request.NoteId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to delete note with ID: {NoteId} - Note may not exist", request.NoteId);
                    throw new InvalidOperationException($"Note with ID {request.NoteId} not found or could not be deleted");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting note with ID: {NoteId}", request.NoteId);
                throw;
            }
        }
    }
}