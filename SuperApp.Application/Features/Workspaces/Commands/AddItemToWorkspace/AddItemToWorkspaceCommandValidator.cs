using FluentValidation;
using SuperAppDataRepositories.Ins;

namespace SuperApp.Application.Features.Workspaces.Commands.AddItemToWorkspace
{
    public class AddItemToWorkspaceCommandValidator : AbstractValidator<AddItemToWorkspaceCommand>
    {
        private readonly IWorkspaceRepository _workspaceRepository;

        public AddItemToWorkspaceCommandValidator(IWorkspaceRepository workspaceRepository)
        {
            _workspaceRepository = workspaceRepository;

            RuleFor(x => x.WorkspaceId)
                .GreaterThan(0)
                .WithMessage("WorkspaceId must be greater than 0");

            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("UserId must be greater than 0");

            RuleFor(x => x.ChildType)
                .NotEmpty()
                .WithMessage("ChildType is required")
                .Must(BeValidChildType)
                .WithMessage("ChildType must be 'tag' or 'note'");

            RuleFor(x => x.ChildId)
                .GreaterThan(0)
                .WithMessage("ChildId must be greater than 0");

            RuleFor(x => x.ParentTagId)
                .GreaterThan(0)
                .WithMessage("ParentTagId must be greater than 0 when specified")
                .When(x => x.ParentTagId.HasValue);

            RuleFor(x => x.RelationshipType)
                .MaximumLength(50)
                .WithMessage("RelationshipType cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.RelationshipType));

            RuleFor(x => x.Label)
                .MaximumLength(255)
                .WithMessage("Label cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Label));

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters")
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
                .WithMessage("SortOrder must be greater than or equal to 0");

            // Async validation: Check if item already exists
            // Note: Only validates when ChildId is provided (existing tag/note)
            // When creating new tag (no ChildId), this check is skipped
            // Repository will perform final validation after tag creation
            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    // Skip check if ChildId not provided (will create new tag)
                    if (!command.ChildId.HasValue || command.ChildId.Value <= 0)
                        return true;
                    
                    // Check if this exact item already exists in workspace
                    var exists = await _workspaceRepository.ItemExistsAsync(
                        command.WorkspaceId,
                        command.ParentTagId,
                        command.ChildType,
                        command.ChildId.Value);
                    
                    return !exists; // Valid if NOT exists
                })
                .WithMessage(command =>
                {
                    return $"Item already exists in workspace: {command.ChildType} with ID {command.ChildId} " +
                           $"under parent {(command.ParentTagId.HasValue ? $"tag {command.ParentTagId}" : "root")}";
                });
        }

        private bool BeValidChildType(string childType)
        {
            if (string.IsNullOrEmpty(childType))
                return false;

            var validTypes = new[] { "tag", "note" };
            return validTypes.Contains(childType.ToLower());
        }
    }
}
