using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Notes.Commands.CreateNote
{
    public record CreateNoteCommand(CreateNoteRequest Request) : IRequest<NoteResponse>;
}