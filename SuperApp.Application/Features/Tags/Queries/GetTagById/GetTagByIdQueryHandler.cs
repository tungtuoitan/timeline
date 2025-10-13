using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Tags.Queries.GetTagById;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetTagById
{
    public class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, TagResponse?>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetTagByIdQueryHandler> _logger;

        public GetTagByIdQueryHandler(
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<GetTagByIdQueryHandler> logger)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TagResponse?> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting tag with ID: {TagId}", request.TagId);

                var tag = await _tagRepository.GetTagById(request.TagId);
                if (tag == null)
                {
                    _logger.LogWarning("Tag with ID {TagId} not found", request.TagId);
                    return null;
                }

                var response = _mapper.Map<TagResponse>(tag);
                _logger.LogInformation("Successfully retrieved tag with ID: {TagId}", request.TagId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tag with ID: {TagId}", request.TagId);
                throw;
            }
        }
    }
}