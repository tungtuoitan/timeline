using MediatR;

namespace SuperApp.Application.Features.Workspaces.Commands.RemoveWorkspaceItem
{
    /// <summary>
    /// Command to remove an item from workspace (soft delete)
    /// Removes only the workspace_items relationship, NOT the actual tag/note
    /// </summary>
    public record RemoveWorkspaceItemCommand : IRequest<bool>
    {
        public int ItemId { get; init; }
        public int UserId { get; init; }  // User making the request
        public bool DeleteDescendants { get; init; } = true;  // Delete all child items too
    }
}
