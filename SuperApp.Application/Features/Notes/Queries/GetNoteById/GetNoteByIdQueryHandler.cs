using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Notes.Queries.GetNoteById;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Notes.Queries.GetNoteById
{
    public class GetNoteByIdQueryHandler : IRequestHandler<GetNoteByIdQuery, NoteResponse>
    {
        private readonly INoteRepository _noteRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetNoteByIdQueryHandler> _logger;

        public GetNoteByIdQueryHandler(
            INoteRepository noteRepository,
            IMapper mapper,
            ILogger<GetNoteByIdQueryHandler> logger)
        {
            _noteRepository = noteRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<NoteResponse> Handle(GetNoteByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting note with ID: {NoteId}", request.NoteId);

                var note = await _noteRepository.GetNoteById(request.NoteId);

                if (note != null)
                {
                    var response = _mapper.Map<NoteResponse>(note);
                    _logger.LogInformation("Successfully retrieved note with ID: {NoteId}", request.NoteId);
                    return response;
                }

                _logger.LogWarning("Note not found with ID: {NoteId}", request.NoteId);
                throw new InvalidOperationException($"Note not found with ID: {request.NoteId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting note with ID: {NoteId}", request.NoteId);
                throw;
            }
        }
    }
}