using FluentValidation;
using SuperApp.Application.Features.Tags.Commands.UpdateTag;

namespace SuperApp.Application.Features.Tags.Commands.UpdateTag
{
    public class UpdateTagValidator : AbstractValidator<UpdateTagCommand>
    {
        public UpdateTagValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Request cannot be null");

            RuleFor(x => x.Request.TagId)
                .GreaterThan(0)
                .WithMessage("Tag ID must be greater than 0")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Name)
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.Name));

            RuleFor(x => x.Request.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .WithMessage("Color must be in hex format (#RRGGBB)")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.Color));
        }
    }
}