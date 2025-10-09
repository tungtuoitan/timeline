using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.StandardRegistry.Queries.GetRegistryById;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.StandardRegistry.Queries.GetRegistryById
{
    public class GetRegistryByIdQueryHandler : IRequestHandler<GetRegistryByIdQuery, StandardRegistryResponse>
    {
        private readonly IStandardRegistryRepository _standardRegistryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetRegistryByIdQueryHandler> _logger;

        public GetRegistryByIdQueryHandler(
            IStandardRegistryRepository standardRegistryRepository,
            IMapper mapper,
            ILogger<GetRegistryByIdQueryHandler> logger)
        {
            _standardRegistryRepository = standardRegistryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<StandardRegistryResponse> Handle(GetRegistryByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting standard registry with ID: {Id}", request.Id);

                var registry = await _standardRegistryRepository.GetStandardRegistryById(request.Id);

                if (registry != null)
                {
                    var response = _mapper.Map<StandardRegistryResponse>(registry);
                    _logger.LogInformation("Successfully retrieved standard registry with ID: {Id}", request.Id);
                    return response;
                }

                _logger.LogWarning("Standard registry not found with ID: {Id}", request.Id);
                throw new InvalidOperationException($"Standard registry not found with ID: {request.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting standard registry with ID: {Id}", request.Id);
                throw;
            }
        }
    }
}