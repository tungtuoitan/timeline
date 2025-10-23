using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Common.Exceptions;
using SuperAppDataRepositories.Ins;

namespace SuperApp.Application.Features.Tags.Commands.BatchMoveTag
{
    /// <summary>
    /// Handler for batch moving multiple tags
    /// </summary>
    public class BatchMoveTagCommandHandler : IRequestHandler<BatchMoveTagCommand, Unit>
    {
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<BatchMoveTagCommandHandler> _logger;

        public BatchMoveTagCommandHandler(
            ITagRepository tagRepository,
            ILogger<BatchMoveTagCommandHandler> logger)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Unit> Handle(BatchMoveTagCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Batch moving {Count} tags to parent {ParentId} at index {StartIndex} for user {UserId}",
                    request.Request.TagIds.Length,
                    request.Request.NewParentId ?? 0,
                    request.Request.StartIndex,
                    request.UserId);

                // Validate that we have tags to move
                if (request.Request.TagIds == null || request.Request.TagIds.Length == 0)
                {
                    throw new ValidationException("No tag IDs provided for batch move");
                }

                // Call repository method to perform batch move
                await _tagRepository.BatchMoveTagsAsync(
                    request.Request.TagIds,
                    request.Request.NewParentId,
                    request.Request.StartIndex,
                    request.UserId);

                _logger.LogInformation(
                    "Successfully batch moved {Count} tags for user {UserId}",
                    request.Request.TagIds.Length,
                    request.UserId);

                return Unit.Value;
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Tag not found during batch move for user {UserId}", request.UserId);
                throw;
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed during batch move for user {UserId}", request.UserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while batch moving tags for user {UserId}", request.UserId);
                throw;
            }
        }
    }
}
