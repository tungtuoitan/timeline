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
                .GreaterThanOrEqualTo(0)
                .WithMessage("Note ID must be greater than or equal to 0")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Name)
                .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.Name));

            RuleFor(x => x.Request.Description)
                .MaximumLength(5000)
                .WithMessage("Description cannot exceed 5000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Tags)
                .MaximumLength(500)
                .WithMessage("Tags cannot exceed 500 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Type)
                .MaximumLength(100)
                .WithMessage("Type cannot exceed 100 characters")
                .When(x => x.Request != null);
        }
    }
}