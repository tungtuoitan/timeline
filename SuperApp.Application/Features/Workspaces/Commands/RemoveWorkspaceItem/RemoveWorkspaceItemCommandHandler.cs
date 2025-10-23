using MediatR;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;

namespace SuperApp.Application.Features.Workspaces.Commands.RemoveWorkspaceItem
{
    public class RemoveWorkspaceItemCommandHandler : IRequestHandler<RemoveWorkspaceItemCommand, bool>
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ILogger<RemoveWorkspaceItemCommandHandler> _logger;

        public RemoveWorkspaceItemCommandHandler(
            IWorkspaceRepository workspaceRepository,
            ILogger<RemoveWorkspaceItemCommandHandler> logger)
        {
            _workspaceRepository = workspaceRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(RemoveWorkspaceItemCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Removing workspace item {ItemId} by user {UserId} (deleteDescendants: {DeleteDescendants})",
                request.ItemId, request.UserId, request.DeleteDescendants);

            // Validate input
            if (request.ItemId <= 0)
            {
                throw new ArgumentException("Item ID must be greater than 0", nameof(request.ItemId));
            }

            // Remove item from workspace (soft delete)
            // This only removes the workspace_items relationship, NOT the actual tag/note
            var success = await _workspaceRepository.RemoveItemFromWorkspaceAsync(
                request.ItemId,
                request.DeleteDescendants);

            if (!success)
            {
                _logger.LogWarning("Workspace item {ItemId} not found or already deleted", request.ItemId);
                return false;
            }

            _logger.LogInformation("Successfully removed workspace item {ItemId}", request.ItemId);
            return true;
        }
    }
}
