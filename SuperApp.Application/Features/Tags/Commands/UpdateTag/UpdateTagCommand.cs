using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Commands.UpdateTag
{
    public class UpdateTagCommand : IRequest<TagResponse>
    {
        public UpdateTagRequest Request { get; set; }

        public UpdateTagCommand(UpdateTagRequest request)
        {
            Request = request;
        }
    }
}