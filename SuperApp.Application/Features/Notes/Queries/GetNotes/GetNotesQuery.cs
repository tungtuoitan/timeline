using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Notes.Queries.GetNotes
{
    public record GetNotesQuery(bool GetAll, string? SearchText, List<int>? TagIds = null) : IRequest<List<NoteResponse>>;
}