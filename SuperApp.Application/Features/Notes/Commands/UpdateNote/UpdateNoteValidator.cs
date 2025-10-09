using FluentValidation;
using SuperApp.Application.Features.Notes.Commands.UpdateNote;

namespace SuperApp.Application.Features.Notes.Commands.UpdateNote
{
    public class UpdateNoteValidator : AbstractValidator<UpdateNoteCommand>
    {
        public UpdateNoteValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Request cannot be null");

            RuleFor(x => x.Request.NoteId)
                .GreaterThan(0)
                .WithMessage("Note ID must be greater than 0")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title cannot exceed 200 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Content)
                .MaximumLength(5000)
                .WithMessage("Content cannot exceed 5000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Tags)
                .MaximumLength(500)
                .WithMessage("Tags cannot exceed 500 characters")
                .When(x => x.Request != null);
        }
    }
}