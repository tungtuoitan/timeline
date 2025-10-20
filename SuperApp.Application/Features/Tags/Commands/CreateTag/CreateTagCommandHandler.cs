using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Tags.Commands.CreateTag;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Features.Tags.Commands.CreateTag
{
    public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, TagResponse>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateTagCommandHandler> _logger;

        public CreateTagCommandHandler(
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<CreateTagCommandHandler> logger)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TagResponse> Handle(CreateTagCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating new tag with name: {Name} for user: {UserId}", 
                    request.Request.Name, request.Request.UserId);

                // Validate that UserId is provided
                if (request.Request.UserId <= 0)
                {
                    throw new ArgumentException("UserId must be provided and greater than 0");
                }

                var tag = _mapper.Map<Tag>(request.Request);
                var createdTag = await _tagRepository.CreateTagAsync(tag);
                
                var response = _mapper.Map<TagResponse>(createdTag);
                
                _logger.LogInformation("Successfully created tag with ID: {TagId} for user: {UserId}", 
                    createdTag.TagId, request.Request.UserId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating tag for user: {UserId}", 
                    request.Request.UserId);
                throw;
            }
        }
    }
}