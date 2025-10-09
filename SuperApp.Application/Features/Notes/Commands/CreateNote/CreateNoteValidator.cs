using FluentValidation;
using SuperApp.Application.Features.Notes.Commands.CreateNote;

namespace SuperApp.Application.Features.Notes.Commands.CreateNote
{
    public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
    {
        public CreateNoteValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Request cannot be null");

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