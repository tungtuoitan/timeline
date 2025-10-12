using FluentValidation;
using SuperApp.Application.Features.Notes.Commands.DeleteNote;

namespace SuperApp.Application.Features.Notes.Commands.DeleteNote
{
    public class DeleteNoteValidator : AbstractValidator<DeleteNoteCommand>
    {
        public DeleteNoteValidator()
        {
            RuleFor(x => x.NoteIds)
                .NotEmpty()
                .WithMessage("Note IDs cannot be empty")
                .Must(BeValidNoteIds)
                .WithMessage("Note IDs must be a comma-separated list of positive integers");
        }

        private static bool BeValidNoteIds(string noteIds)
        {
            if (string.IsNullOrWhiteSpace(noteIds))
                return false;

            var ids = noteIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            if (ids.Length == 0)
                return false;

            foreach (var id in ids)
            {
                if (!int.TryParse(id.Trim(), out var noteId) || noteId <= 0)
                    return false;
            }

            return true;
        }
    }
}