using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Workspaces.Queries.GetWorkspaceTree
{
    public class GetWorkspaceTreeQuery : IRequest<WorkspaceWithTreeResponse>
    {
        public int WorkspaceId { get; set; }
        public int UserId { get; set; }

        public GetWorkspaceTreeQuery(int workspaceId, int userId)
        {
            WorkspaceId = workspaceId;
            UserId = userId;
        }
    }
}
