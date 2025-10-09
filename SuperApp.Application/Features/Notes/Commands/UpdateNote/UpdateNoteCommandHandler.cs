using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Notes.Commands.UpdateNote;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Features.Notes.Commands.UpdateNote
{
    public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand, NoteResponse>
    {
        private readonly INoteRepository _noteRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateNoteCommandHandler> _logger;

        public UpdateNoteCommandHandler(
            INoteRepository noteRepository,
            IMapper mapper,
            ILogger<UpdateNoteCommandHandler> logger)
        {
            _noteRepository = noteRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<NoteResponse> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Updating note with ID: {NoteId}", request.Request.NoteId);

                var note = _mapper.Map<Note>(request.Request);
                note.NoteId = request.Request.NoteId;
                
                await _noteRepository.IuNote(note);
                
                var response = _mapper.Map<NoteResponse>(note);
                
                _logger.LogInformation("Successfully updated note with ID: {NoteId}", request.Request.NoteId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating note with ID: {NoteId}", request.Request.NoteId);
                throw;
            }
        }
    }
}