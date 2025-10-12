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
                _logger.LogInformation("Deleting notes with IDs: {NoteIds}", request.NoteIds);

                var result = await _noteRepository.DeleteNoteAsync(request.NoteIds);

                _logger.LogInformation("Successfully deleted notes with IDs: {NoteIds}", request.NoteIds);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting notes with IDs: {NoteIds}", request.NoteIds);
                throw;
            }
        }
    }
}