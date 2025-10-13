using FluentValidation;
using SuperApp.Application.Features.Tags.Commands.DeleteTag;

namespace SuperApp.Application.Features.Tags.Commands.DeleteTag
{
    public class DeleteTagValidator : AbstractValidator<DeleteTagCommand>
    {
        public DeleteTagValidator()
        {
            RuleFor(x => x.TagId)
                .GreaterThan(0)
                .WithMessage("Tag ID must be greater than 0");
        }
    }
}