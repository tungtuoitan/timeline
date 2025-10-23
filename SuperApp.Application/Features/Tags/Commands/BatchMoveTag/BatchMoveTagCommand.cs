using MediatR;
using SuperAppModels.DTOs.Requests;

namespace SuperApp.Application.Features.Tags.Commands.BatchMoveTag
{
    /// <summary>
    /// Command for batch moving multiple tags to a new parent/position
    /// </summary>
    public class BatchMoveTagCommand : IRequest<Unit>
    {
        public BatchMoveTagRequest Request { get; set; }
        public int UserId { get; set; }

        public BatchMoveTagCommand(BatchMoveTagRequest request, int userId)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            UserId = userId;
        }
    }
}
