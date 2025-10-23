using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Workspaces.Commands.UpdateWorkspaceItem
{
    public class UpdateWorkspaceItemCommandHandler : IRequestHandler<UpdateWorkspaceItemCommand, UpdateWorkspaceItemResponse>
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateWorkspaceItemCommandHandler> _logger;

        public UpdateWorkspaceItemCommandHandler(
            IWorkspaceRepository workspaceRepository,
            IMapper mapper,
            ILogger<UpdateWorkspaceItemCommandHandler> logger)
        {
            _workspaceRepository = workspaceRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UpdateWorkspaceItemResponse> Handle(UpdateWorkspaceItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Updating workspace item. ItemId: {ItemId}, UserId: {UserId}, Label: {Label}, Color: {Color}, Icon: {Icon}, SortOrder: {SortOrder}",
                    request.ItemId, request.UserId, request.Label, request.Color, request.Icon, request.SortOrder);

                // Call repository to update item
                var updatedItem = await _workspaceRepository.UpdateWorkspaceItemAsync(
                    request.ItemId,
                    request.UserId,
                    request.Label,
                    request.Notes,
                    request.Color,
                    request.Icon,
                    request.SortOrder);

                _logger.LogInformation("Successfully updated workspace item {ItemId}", request.ItemId);

                // Map to response DTO
                var response = _mapper.Map<UpdateWorkspaceItemResponse>(updatedItem);
                response.Message = "Workspace item updated successfully";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating workspace item. ItemId: {ItemId}, UserId: {UserId}",
                    request.ItemId, request.UserId);
                throw;
            }
        }
    }
}
