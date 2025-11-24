using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Features.Notes.Commands.CreateNote
{
    public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, NoteResponse>
    {
        private readonly INoteRepository _noteRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateNoteCommandHandler> _logger;

        public CreateNoteCommandHandler(
            INoteRepository noteRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<CreateNoteCommandHandler> logger)
        {
            _noteRepository = noteRepository;
            _userRepository = userRepository;
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
                    var user = await _userRepository.GetByEmailAsync(request.Request.CreatedBy);
                    createdByUserId = user?.UserId;
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
