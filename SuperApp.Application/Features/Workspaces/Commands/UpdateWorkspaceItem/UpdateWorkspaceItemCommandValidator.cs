using FluentValidation;

namespace SuperApp.Application.Features.Workspaces.Commands.UpdateWorkspaceItem
{
    public class UpdateWorkspaceItemCommandValidator : AbstractValidator<UpdateWorkspaceItemCommand>
    {
        public UpdateWorkspaceItemCommandValidator()
        {
            RuleFor(x => x.ItemId)
                .GreaterThan(0)
                .WithMessage("ItemId must be greater than 0");

            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("UserId must be greater than 0");

            RuleFor(x => x.Label)
                .MaximumLength(200)
                .WithMessage("Label cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Label));

            RuleFor(x => x.Notes)
                .MaximumLength(2000)
                .WithMessage("Notes cannot exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));

            RuleFor(x => x.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .WithMessage("Color must be in hex format (#RRGGBB)")
                .When(x => !string.IsNullOrEmpty(x.Color));

            RuleFor(x => x.Icon)
                .MaximumLength(50)
                .WithMessage("Icon cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Icon));

            RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SortOrder must be non-negative")
                .When(x => x.SortOrder.HasValue);

            // At least one field must be provided
            RuleFor(x => x)
                .Must(HaveAtLeastOneField)
                .WithMessage("At least one field (Label, Notes, Color, Icon, or SortOrder) must be provided for update");
        }

        private bool HaveAtLeastOneField(UpdateWorkspaceItemCommand command)
        {
            return !string.IsNullOrEmpty(command.Label) ||
                   !string.IsNullOrEmpty(command.Notes) ||
                   !string.IsNullOrEmpty(command.Color) ||
                   !string.IsNullOrEmpty(command.Icon) ||
                   command.SortOrder.HasValue;
        }
    }
}
