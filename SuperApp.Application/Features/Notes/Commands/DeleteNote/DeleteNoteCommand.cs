using MediatR;

namespace SuperApp.Application.Features.Notes.Commands.DeleteNote
{
    public record DeleteNoteCommand(string NoteIds) : IRequest<bool>;
}