using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Notes.Queries.GetNotes;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Notes.Queries.GetNotes
{
    public class GetNotesQueryHandler : IRequestHandler<GetNotesQuery, List<NoteResponse>>
    {
        private readonly INoteRepository _noteRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetNotesQueryHandler> _logger;

        public GetNotesQueryHandler(
            INoteRepository noteRepository,
            IMapper mapper,
            ILogger<GetNotesQueryHandler> logger)
        {
            _noteRepository = noteRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<NoteResponse>> Handle(GetNotesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting notes with GetAll: {GetAll}, SearchText: {SearchText}", 
                    request.GetAll, request.SearchText);

                var notes = await _noteRepository.GetNotes(request.GetAll, request.SearchText ?? string.Empty);
                var response = _mapper.Map<List<NoteResponse>>(notes);

                _logger.LogInformation("Successfully retrieved {Count} notes", response.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting notes");
                throw;
            }
        }
    }
}