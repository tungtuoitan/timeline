using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    /// <summary>
    /// Repository interface for standard registry data access
    /// </summary>
    public interface IStandardRegistryRepository
    {
        /// <summary>
        /// Gets standard registry entries with optional type filtering
        /// </summary>
        /// <param name="type">Optional registry type to filter by. If null, returns all registries.</param>
        /// <param name="showAll">If true, returns all entries including inactive ones. If false, returns only active entries.</param>
        /// <returns>List of standard registry entries matching the criteria</returns>
        Task<List<StandardRegistry>> GetByTypeAsync(string? type = null, bool showAll = false);

        Task SetJsonDetailAsync(string code, string type, string jsonDetail);
    }
}
