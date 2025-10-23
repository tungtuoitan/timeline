using FluentValidation;

namespace SuperApp.Application.Features.Workspaces.Commands.MoveWorkspaceItem
{
    public class MoveWorkspaceItemCommandValidator : AbstractValidator<MoveWorkspaceItemCommand>
    {
        public MoveWorkspaceItemCommandValidator()
        {
            RuleFor(x => x.ItemId)
                .GreaterThan(0)
                .WithMessage("ItemId must be greater than 0");

            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("UserId must be greater than 0");

            RuleFor(x => x.NewParentTagId)
                .GreaterThan(0)
                .WithMessage("NewParentTagId must be greater than 0 when specified")
                .When(x => x.NewParentTagId.HasValue);

            RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SortOrder must be non-negative")
                .When(x => x.SortOrder.HasValue);
        }
    }
}
