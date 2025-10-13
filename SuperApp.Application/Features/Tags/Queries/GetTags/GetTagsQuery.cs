using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetTags
{
    public class GetTagsQuery : IRequest<List<TagResponse>>
    {
        public int UserId { get; set; }

        public GetTagsQuery(int userId)
        {
            UserId = userId;
        }
    }
}