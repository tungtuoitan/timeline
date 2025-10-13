using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Tags.Queries.GetTags;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetTags
{
    public class GetTagsQueryHandler : IRequestHandler<GetTagsQuery, List<TagResponse>>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetTagsQueryHandler> _logger;

        public GetTagsQueryHandler(
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<GetTagsQueryHandler> logger)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<TagResponse>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting tags for UserId: {UserId}", request.UserId);

                var tags = await _tagRepository.GetTags(request.UserId);
                var response = _mapper.Map<List<TagResponse>>(tags);

                _logger.LogInformation("Successfully retrieved {Count} tags", response.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tags");
                throw;
            }
        }
    }
}