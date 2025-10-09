using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.StandardRegistry.Queries.GetAllRegistries;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.StandardRegistry.Queries.GetAllRegistries
{
    public class GetAllRegistriesQueryHandler : IRequestHandler<GetAllRegistriesQuery, List<StandardRegistryResponse>>
    {
        private readonly IStandardRegistryRepository _standardRegistryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllRegistriesQueryHandler> _logger;

        public GetAllRegistriesQueryHandler(
            IStandardRegistryRepository standardRegistryRepository,
            IMapper mapper,
            ILogger<GetAllRegistriesQueryHandler> logger)
        {
            _standardRegistryRepository = standardRegistryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<StandardRegistryResponse>> Handle(GetAllRegistriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all standard registries");

                var registries = await _standardRegistryRepository.GetAllStandardRegistry();
                var response = _mapper.Map<List<StandardRegistryResponse>>(registries);

                _logger.LogInformation("Successfully retrieved {Count} standard registries", response.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all standard registries");
                throw;
            }
        }
    }
}