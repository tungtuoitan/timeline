using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Notes.Commands.CreateNote;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Mos;

namespace SuperApp.Application.Features.Notes.Commands.CreateNote
{
    public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, NoteResponse>
    {
        private readonly INoteRepository _noteRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateNoteCommandHandler> _logger;

        public CreateNoteCommandHandler(
            INoteRepository noteRepository,
            IMapper mapper,
            ILogger<CreateNoteCommandHandler> logger)
        {
            _noteRepository = noteRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<NoteResponse> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating new note with name: {Name}", request.Request.Name);

                var note = _mapper.Map<Note>(request.Request);
                var notesResult = await _noteRepository.IuNote(note);
                
                // Get the created note from the result
                var createdNote = notesResult.Notes?.FirstOrDefault();
                if (createdNote != null)
                {
                    note.NoteId = createdNote.NoteId;
                }
                
                var response = _mapper.Map<NoteResponse>(note);
                
                _logger.LogInformation("Successfully created note with ID: {NoteId}", note.NoteId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating note");
                throw;
            }
        }
    }
}