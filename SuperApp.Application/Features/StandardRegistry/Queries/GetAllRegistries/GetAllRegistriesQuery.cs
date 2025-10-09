using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.StandardRegistry.Queries.GetAllRegistries
{
    public record GetAllRegistriesQuery() : IRequest<List<StandardRegistryResponse>>;
}