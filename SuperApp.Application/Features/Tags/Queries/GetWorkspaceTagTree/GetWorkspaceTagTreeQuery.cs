using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetWorkspaceTagTree
{
    public class GetWorkspaceTagTreeQuery : IRequest<WorkspaceWithTagTreeResponse>
    {
        public int WorkspaceId { get; set; }
        public int UserId { get; set; }

        public GetWorkspaceTagTreeQuery(int workspaceId, int userId)
        {
            WorkspaceId = workspaceId;
            UserId = userId;
        }
    }
}