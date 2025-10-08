using MediatR;

namespace SuperApp.Application.Features.Notes.Commands.DeleteNote
{
    public record DeleteNoteCommand(int NoteId) : IRequest<bool>;
}