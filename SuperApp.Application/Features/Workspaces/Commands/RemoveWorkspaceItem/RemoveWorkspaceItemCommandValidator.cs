using FluentValidation;

namespace SuperApp.Application.Features.Workspaces.Commands.RemoveWorkspaceItem
{
    public class RemoveWorkspaceItemCommandValidator : AbstractValidator<RemoveWorkspaceItemCommand>
    {
        public RemoveWorkspaceItemCommandValidator()
        {
            RuleFor(x => x.ItemId)
                .GreaterThan(0)
                .WithMessage("Item ID must be greater than 0");

            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("User ID must be greater than 0");
        }
    }
}
