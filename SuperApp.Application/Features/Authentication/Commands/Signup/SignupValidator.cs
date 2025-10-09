using FluentValidation;
using SuperApp.Application.Features.Authentication.Commands.Signup;

namespace SuperApp.Application.Features.Authentication.Commands.Signup
{
    public class SignupValidator : AbstractValidator<SignupCommand>
    {
        public SignupValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Request cannot be null");

            RuleFor(x => x.Request.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format")
                .MaximumLength(255)
                .WithMessage("Email cannot exceed 255 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
                .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")
                .MaximumLength(100)
                .WithMessage("Password cannot exceed 100 characters")
                .When(x => x.Request != null);

            RuleFor(x => x.Request.Phone)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Invalid phone number format")
                .When(x => x.Request != null && !string.IsNullOrWhiteSpace(x.Request.Phone));
        }
    }
}