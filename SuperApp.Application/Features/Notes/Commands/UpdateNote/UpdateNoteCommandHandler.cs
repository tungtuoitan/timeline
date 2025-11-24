using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
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
                _logger.LogInformation("Processing note with ID: {NoteId}, Name: '{Name}', TagIds: [{TagIds}]",
                    request.Request.NoteId,
                    request.Request.Name,
                    request.Request.TagIds != null ? string.Join(",", request.Request.TagIds) : "null");

                var note = _mapper.Map<Note>(request.Request);
                note.NoteId = request.Request.NoteId;

                Note resultNote;

                if (request.Request.NoteId == 0)
                {
                    _logger.LogInformation("Creating new note with name: '{Name}'", request.Request.Name);
                    resultNote = await _noteRepository.CreateNoteAsync(note, request.Request.TagIds, null);
                }
                else
                {
                    _logger.LogInformation("Updating existing note with ID: {NoteId}, Name: '{Name}'",
                        request.Request.NoteId, request.Request.Name);
                    resultNote = await _noteRepository.UpdateNoteAsync(note, request.Request.TagIds, null);
                }

                var response = _mapper.Map<NoteResponse>(resultNote);

                _logger.LogInformation("Successfully processed note with ID: {NoteId}, final TagCount: {TagCount}",
                    resultNote.NoteId, response.Tags?.Count ?? 0);
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while processing note with ID: {NoteId}", request.Request.NoteId);
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while processing note with ID: {NoteId}", request.Request.NoteId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while processing note with ID: {NoteId}", request.Request.NoteId);
                throw;
            }
        }
    }
}
