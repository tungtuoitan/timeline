using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.StandardRegistry.Queries.GetRegistryById
{
    public record GetRegistryByIdQuery(int Id) : IRequest<StandardRegistryResponse>;
}