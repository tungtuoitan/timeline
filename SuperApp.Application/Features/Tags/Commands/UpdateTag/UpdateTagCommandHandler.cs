using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Tags.Commands.UpdateTag;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Features.Tags.Commands.UpdateTag
{
    public class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, TagResponse>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateTagCommandHandler> _logger;

        public UpdateTagCommandHandler(
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<UpdateTagCommandHandler> logger)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TagResponse> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Updating tag with ID: {TagId}", request.Request.TagId);

                // Get existing tag
                var existingTag = await _tagRepository.GetTagById(request.Request.TagId);
                if (existingTag == null)
                {
                    throw new InvalidOperationException($"Tag with ID {request.Request.TagId} not found");
                }

                // Map update request to existing tag
                var tagToUpdate = _mapper.Map<Tag>(request.Request);
                tagToUpdate.CreatedBy = existingTag.CreatedBy; // Preserve original creator
                tagToUpdate.CreatedAt = existingTag.CreatedAt; // Preserve original creation date
                tagToUpdate.UserId = existingTag.UserId; // Preserve user ID
                tagToUpdate.ParentId = existingTag.ParentId; // Preserve parent ID
                tagToUpdate.Path = existingTag.Path; // Preserve path
                tagToUpdate.Slug = existingTag.Slug; // Preserve slug
                tagToUpdate.Icon = existingTag.Icon; // Preserve icon
                tagToUpdate.IsPublic = existingTag.IsPublic; // Preserve public status
                tagToUpdate.PublicSlug = existingTag.PublicSlug; // Preserve public slug
                tagToUpdate.DeletedAt = existingTag.DeletedAt; // Preserve deleted status

                var updatedTag = await _tagRepository.UpdateTagAsync(tagToUpdate);
                var response = _mapper.Map<TagResponse>(updatedTag);

                _logger.LogInformation("Successfully updated tag with ID: {TagId}", updatedTag.Id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating tag with ID: {TagId}", request.Request.TagId);
                throw;
            }
        }
    }
}