using FluentValidation;
using SuperApp.Application.Features.Tags.Commands.CreateTag;

namespace SuperApp.Application.Features.Tags.Commands.CreateTag
{
    public class CreateTagValidator : AbstractValidator<CreateTagCommand>
    {
        public CreateTagValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Request cannot be null");

            RuleFor(x => x.Request.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(255)
                .WithMessage("Name cannot exceed 255 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .WithMessage("Color must be in hex format (#RRGGBB)")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.Color));

            RuleFor(x => x.Request.Slug)
                .MaximumLength(255)
                .WithMessage("Slug cannot exceed 255 characters")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.Slug));

            RuleFor(x => x.Request.Icon)
                .MaximumLength(50)
                .WithMessage("Icon cannot exceed 50 characters")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.Icon));

            RuleFor(x => x.Request.PublicSlug)
                .MaximumLength(255)
                .WithMessage("Public slug cannot exceed 255 characters")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.PublicSlug));

            RuleFor(x => x.Request.UserId)
                .GreaterThan(0)
                .WithMessage("UserId must be greater than 0")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.ParentId)
                .GreaterThan(0)
                .WithMessage("ParentId must be greater than 0 when specified")
                .When(x => x.Request != null && x.Request.ParentId.HasValue);
        }
    }
}