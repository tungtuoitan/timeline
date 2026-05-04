using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for managing standard registry operations
    /// </summary>
    public interface IStandardRegistryService
    {
        /// <summary>
        /// Gets standard registry entries with optional type filtering
        /// </summary>
        /// <param name="type">Optional registry type to filter by. If null, returns all registries.</param>
        /// <param name="showAll">If true, returns all entries including inactive ones. If false, returns only active entries.</param>
        /// <returns>List of standard registry entries matching the criteria</returns>
        Task<List<StandardRegistry>> GetStandardRegistries(string? type = null, bool showAll = false);

        /// <summary>Sets the checklist template for a taskType registry entry (updates json_detail).</summary>
        Task SetChecklistTemplateAsync(string taskTypeCode, string template);
    }
}
