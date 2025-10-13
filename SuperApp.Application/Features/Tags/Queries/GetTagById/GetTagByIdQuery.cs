using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetTagById
{
    public class GetTagByIdQuery : IRequest<TagResponse?>
    {
        public int TagId { get; set; }

        public GetTagByIdQuery(int tagId)
        {
            TagId = tagId;
        }
    }
}