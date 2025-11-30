using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Workspaces.Commands.AddItemToWorkspace
{
    /// <summary>
    /// Command to add an item (tag or note) to a workspace
    /// </summary>
    public record AddItemToWorkspaceCommand : IRequest<WorkspaceItemResponse>
    {
        public int WorkspaceId { get; init; }
        public int UserId { get; init; }  // User making the request
        public int? ParentTagId { get; init; }  // Nullable for root items
        public string ChildType { get; init; } = string.Empty;
        public int? ChildId { get; init; }  // Optional when creating new tag
        public string? TagName { get; init; }  // For auto-creating tags
        public bool IsOriginal { get; init; } = true;  // TRUE when creating new item, FALSE when sharing existing item
        public string? RelationshipType { get; init; }
        public string? Label { get; init; }
        public string? Notes { get; init; }
        public int SortOrder { get; init; }
        public string? Color { get; init; }
        public string? Icon { get; init; }
    }
}
