using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Notes.Commands.CreateNote;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using UserProfileDataRepositories.Ins;

namespace SuperApp.Application.Features.Notes.Commands.CreateNote
{
    public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, NoteResponse>
    {
        private readonly INoteRepository _noteRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateNoteCommandHandler> _logger;

        public CreateNoteCommandHandler(
            INoteRepository noteRepository,
            IAuthRepository authRepository,
            IMapper mapper,
            ILogger<CreateNoteCommandHandler> logger)
        {
            _noteRepository = noteRepository;
            _authRepository = authRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<NoteResponse> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating new note with name: {Name}", request.Request.Name);

                var note = _mapper.Map<Note>(request.Request);
                
                // Convert email to user ID
                int? createdByUserId = null;
                if (!string.IsNullOrEmpty(request.Request.CreatedBy))
                {
                    var user = await _authRepository.GetUserByEmailAsync(request.Request.CreatedBy);
                    createdByUserId = user?.Id;
                }
                
                var createdNote = await _noteRepository.CreateNoteAsync(note, request.Request.TagIds, createdByUserId);
                
                var response = _mapper.Map<NoteResponse>(createdNote);
                
                _logger.LogInformation("Successfully created note with ID: {NoteId}", createdNote.NoteId);
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