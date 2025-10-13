using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Tags.Commands.DeleteTag;
using SuperAppDataRepositories.Ins;

namespace SuperApp.Application.Features.Tags.Commands.DeleteTag
{
    public class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand, bool>
    {
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<DeleteTagCommandHandler> _logger;

        public DeleteTagCommandHandler(
            ITagRepository tagRepository,
            ILogger<DeleteTagCommandHandler> logger)
        {
            _tagRepository = tagRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting tag with ID: {TagId}", request.TagId);

                // Check if tag exists
                var existingTag = await _tagRepository.GetTagById(request.TagId);
                if (existingTag == null)
                {
                    _logger.LogWarning("Tag with ID {TagId} not found for deletion", request.TagId);
                    return false;
                }

                var success = await _tagRepository.DeleteTagAsync(request.TagId);
                
                if (success)
                {
                    _logger.LogInformation("Successfully deleted tag with ID: {TagId}", request.TagId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete tag with ID: {TagId}", request.TagId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting tag with ID: {TagId}", request.TagId);
                throw;
            }
        }
    }
}