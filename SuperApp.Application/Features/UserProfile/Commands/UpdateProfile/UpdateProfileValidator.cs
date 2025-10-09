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

            RuleFor(x => x.Request.FullName)
                .MaximumLength(100)
                .WithMessage("Full name cannot exceed 100 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Bio)
                .MaximumLength(1000)
                .WithMessage("Bio cannot exceed 1000 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Address)
                .MaximumLength(500)
                .WithMessage("Address cannot exceed 500 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.ProfilePictureUrl)
                .Must(BeAValidUrl)
                .WithMessage("Invalid URL format for profile picture")
                .When(x => x.Request != null && !string.IsNullOrEmpty(x.Request.ProfilePictureUrl));
        }

        private static bool BeAValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out var result) && 
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
    }
}