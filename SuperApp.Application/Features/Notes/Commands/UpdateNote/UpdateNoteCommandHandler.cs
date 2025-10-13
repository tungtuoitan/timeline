using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Notes.Commands.UpdateNote;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using UserProfileDataRepositories.Ins;

namespace SuperApp.Application.Features.Notes.Commands.UpdateNote
{
    public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand, NoteResponse>
    {
        private readonly INoteRepository _noteRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateNoteCommandHandler> _logger;

        public UpdateNoteCommandHandler(
            INoteRepository noteRepository,
            IAuthRepository authRepository,
            IMapper mapper,
            ILogger<UpdateNoteCommandHandler> logger)
        {
            _noteRepository = noteRepository;
            _authRepository = authRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<NoteResponse> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing note with ID: {NoteId}", request.Request.NoteId);

                var note = _mapper.Map<Note>(request.Request);
                note.NoteId = request.Request.NoteId;
                
                Note resultNote;
                
                // For updates, we don't need to pass user ID as CreatedBy shouldn't change
                if (request.Request.NoteId == 0)
                {
                    _logger.LogInformation("Creating new note");
                    resultNote = await _noteRepository.CreateNoteAsync(note, request.Request.TagIds, null);
                }
                else
                {
                    _logger.LogInformation("Updating existing note with ID: {NoteId}", request.Request.NoteId);
                    resultNote = await _noteRepository.UpdateNoteAsync(note, request.Request.TagIds, null);
                }
                
                var response = _mapper.Map<NoteResponse>(resultNote);
                
                _logger.LogInformation("Successfully processed note with ID: {NoteId}", resultNote.NoteId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing note with ID: {NoteId}", request.Request.NoteId);
                throw;
            }
        }
    }
}