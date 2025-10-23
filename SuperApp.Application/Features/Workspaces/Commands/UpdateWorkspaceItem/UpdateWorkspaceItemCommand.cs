using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Workspaces.Commands.UpdateWorkspaceItem
{
    /// <summary>
    /// Command to update workspace item metadata (label, notes, color, icon, sort order)
    /// </summary>
    public record UpdateWorkspaceItemCommand : IRequest<UpdateWorkspaceItemResponse>
    {
        public int ItemId { get; init; }
        public int UserId { get; init; }  // User making the request
        public string? Label { get; init; }
        public string? Notes { get; init; }
        public string? Color { get; init; }
        public string? Icon { get; init; }
        public int? SortOrder { get; init; }
    }
}
