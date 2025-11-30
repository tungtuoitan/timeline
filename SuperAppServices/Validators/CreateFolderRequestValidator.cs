using FluentValidation;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Validators
{
    /// <summary>
    /// Validator for CreateFolderRequest
    /// </summary>
    public class CreateFolderRequestValidator : AbstractValidator<CreateFolderRequest>
    {
        public CreateFolderRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Folder name is required")
                .MaximumLength(255)
                .WithMessage("Folder name cannot exceed 255 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters")
                .When(x => x.Description != null);

            RuleFor(x => x.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .WithMessage("Color must be a valid hex color code (e.g., #F59E0B)")
                .When(x => x.Color != null);

            RuleFor(x => x.Icon)
                .MaximumLength(50)
                .WithMessage("Icon cannot exceed 50 characters")
                .When(x => x.Icon != null);

            RuleFor(x => x.ParentFolderId)
                .GreaterThan(0)
                .WithMessage("Parent folder ID must be positive")
                .When(x => x.ParentFolderId.HasValue);
        }
    }
}
