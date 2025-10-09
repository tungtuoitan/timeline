using FluentValidation;
using SuperApp.Application.Features.UserProfile.Commands.UpdateProfile;

namespace SuperApp.Application.Features.UserProfile.Commands.UpdateProfile
{
    public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Request cannot be null");

            RuleFor(x => x.UserEmail)
                .NotEmpty()
                .WithMessage("User email is required")
                .EmailAddress()
                .WithMessage("Invalid email format");

            RuleFor(x => x.Request.Email)
                .EmailAddress()
                .WithMessage("Invalid email format")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.Email));

            RuleFor(x => x.Request.AppC)
                .MaximumLength(50)
                .WithMessage("AppC cannot exceed 50 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Parents)
                .MaximumLength(1000)
                .WithMessage("Parents configuration cannot exceed 1000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Priorities)
                .MaximumLength(1000)
                .WithMessage("Priorities configuration cannot exceed 1000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Statuses)
                .MaximumLength(1000)
                .WithMessage("Statuses configuration cannot exceed 1000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Types)
                .MaximumLength(1000)
                .WithMessage("Types configuration cannot exceed 1000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.RepeatTypes)
                .MaximumLength(1000)
                .WithMessage("RepeatTypes configuration cannot exceed 1000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.IsUpdatedTodays)
                .MaximumLength(100)
                .WithMessage("IsUpdatedTodays cannot exceed 100 characters")
                .When(x => x.Request != null);
        }
    }
}