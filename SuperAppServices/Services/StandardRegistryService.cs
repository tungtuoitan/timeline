using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for managing standard registry operations
    /// </summary>
    public class StandardRegistryService : IStandardRegistryService
    {
        private readonly IStandardRegistryRepository _repository;
        private readonly ILogger<StandardRegistryService> _logger;

        public StandardRegistryService(
            IStandardRegistryRepository repository,
            ILogger<StandardRegistryService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets standard registry entries with optional type filtering
        /// </summary>
        /// <param name="type">Optional registry type to filter by. If null, returns all registries.</param>
        /// <param name="showAll">If true, returns all entries including inactive ones. If false, returns only active entries.</param>
        /// <returns>List of standard registry entries matching the criteria</returns>
        public async Task<List<StandardRegistry>> GetStandardRegistries(string? type = null, bool showAll = false)
        {
            try
            {
                _logger.LogInformation("Getting standard registries for type: {Type}, showAll: {ShowAll}", type ?? "ALL", showAll);

                var registries = await _repository.GetByTypeAsync(type, showAll);

                _logger.LogInformation("Successfully retrieved {Count} standard registries for type: {Type}",
                    registries?.Count ?? 0, type ?? "ALL");

                return registries ?? new List<StandardRegistry>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving standard registries for type: {Type}", type ?? "ALL");
                throw;
            }
        }

        public async Task SetChecklistTemplateAsync(string taskTypeCode, string template)
        {
            _logger.LogInformation("Setting checklist template for taskType: {TaskTypeCode}", taskTypeCode);

            // Build json_detail payload
            var jsonDetail = System.Text.Json.JsonSerializer.Serialize(new { checklistTemplate = template });

            await _repository.SetJsonDetailAsync(taskTypeCode, "taskType", jsonDetail);

            _logger.LogInformation("Checklist template set successfully for taskType: {TaskTypeCode}", taskTypeCode);
        }
    }
}
