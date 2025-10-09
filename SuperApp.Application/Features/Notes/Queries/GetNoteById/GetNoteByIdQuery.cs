using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Notes.Queries.GetNoteById
{
    public record GetNoteByIdQuery(int NoteId) : IRequest<NoteResponse>;
}