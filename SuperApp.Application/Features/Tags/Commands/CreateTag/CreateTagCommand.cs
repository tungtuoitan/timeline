using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Commands.CreateTag
{
    public class CreateTagCommand : IRequest<TagResponse>
    {
        public CreateTagRequest Request { get; set; }

        public CreateTagCommand(CreateTagRequest request)
        {
            Request = request;
        }
    }
}