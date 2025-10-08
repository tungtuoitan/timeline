using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Notes.Commands.UpdateNote
{
    public record UpdateNoteCommand(UpdateNoteRequest Request) : IRequest<NoteResponse>;
}