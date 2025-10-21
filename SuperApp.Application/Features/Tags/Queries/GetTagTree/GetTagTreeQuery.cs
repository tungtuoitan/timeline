using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetTagTree
{
    public class GetTagTreeQuery : IRequest<List<TagTreeResponse>>
    {
        public int WorkspaceId { get; set; }
        public int UserId { get; set; }

        public GetTagTreeQuery(int workspaceId, int userId)
        {
            WorkspaceId = workspaceId;
            UserId = userId;
        }
    }
}