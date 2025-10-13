using MediatR;

namespace SuperApp.Application.Features.Tags.Commands.DeleteTag
{
    public class DeleteTagCommand : IRequest<bool>
    {
        public int TagId { get; set; }

        public DeleteTagCommand(int tagId)
        {
            TagId = tagId;
        }
    }
}