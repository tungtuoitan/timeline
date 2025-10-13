using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetTagTree
{
    public class GetTagTreeQuery : IRequest<List<TagTreeResponse>>
    {
        public int UserId { get; set; }
        public bool IncludeShared { get; set; } = true;

        public GetTagTreeQuery(int userId, bool includeShared = true)
        {
            UserId = userId;
            IncludeShared = includeShared;
        }
    }
}