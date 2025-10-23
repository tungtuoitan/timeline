using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Workspaces.Commands.MoveWorkspaceItem
{
    public class MoveWorkspaceItemCommandHandler : IRequestHandler<MoveWorkspaceItemCommand, UpdateWorkspaceItemResponse>
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<MoveWorkspaceItemCommandHandler> _logger;

        public MoveWorkspaceItemCommandHandler(
            IWorkspaceRepository workspaceRepository,
            IMapper mapper,
            ILogger<MoveWorkspaceItemCommandHandler> logger)
        {
            _workspaceRepository = workspaceRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UpdateWorkspaceItemResponse> Handle(MoveWorkspaceItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Moving workspace item. ItemId: {ItemId}, UserId: {UserId}, NewParentTagId: {NewParentTagId}, SortOrder: {SortOrder}",
                    request.ItemId, request.UserId, request.NewParentTagId, request.SortOrder);

                // Call repository to move item
                var movedItem = await _workspaceRepository.MoveWorkspaceItemAsync(
                    request.ItemId,
                    request.UserId,
                    request.NewParentTagId,
                    request.SortOrder);

                _logger.LogInformation(
                    "Successfully moved workspace item {ItemId} to parent {ParentId}", 
                    request.ItemId, 
                    request.NewParentTagId);

                // Map to response DTO
                var response = _mapper.Map<UpdateWorkspaceItemResponse>(movedItem);
                response.Message = "Workspace item moved successfully";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while moving workspace item. ItemId: {ItemId}, UserId: {UserId}, NewParentTagId: {NewParentTagId}",
                    request.ItemId, request.UserId, request.NewParentTagId);
                throw;
            }
        }
    }
}
