using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Workspaces.Commands.MoveWorkspaceItem
{
    /// <summary>
    /// Command to move a workspace item to a different parent tag
    /// </summary>
    public record MoveWorkspaceItemCommand : IRequest<UpdateWorkspaceItemResponse>
    {
        public int ItemId { get; init; }
        public int UserId { get; init; }  // User making the request
        public int? NewParentTagId { get; init; }  // Nullable to allow moving to root
        public int? SortOrder { get; init; }
    }
}
