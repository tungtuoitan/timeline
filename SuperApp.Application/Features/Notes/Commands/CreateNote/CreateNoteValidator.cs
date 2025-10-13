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

            RuleFor(x => x.Request.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Description)
                .MaximumLength(5000)
                .WithMessage("Description cannot exceed 5000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Type)
                .MaximumLength(100)
                .WithMessage("Type cannot exceed 100 characters")
                .When(x => x.Request != null);
        }
    }
}