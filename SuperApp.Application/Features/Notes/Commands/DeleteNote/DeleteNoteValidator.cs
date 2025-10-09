using FluentValidation;
using SuperApp.Application.Features.Notes.Commands.DeleteNote;

namespace SuperApp.Application.Features.Notes.Commands.DeleteNote
{
    public class DeleteNoteValidator : AbstractValidator<DeleteNoteCommand>
    {
        public DeleteNoteValidator()
        {
            RuleFor(x => x.NoteId)
                .GreaterThan(0)
                .WithMessage("Note ID must be greater than 0");
        }
    }
}