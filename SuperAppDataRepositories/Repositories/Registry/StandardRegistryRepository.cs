using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;
using SuperAppModels.Utils;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for standard registry data access
    /// </summary>
    public class StandardRegistryRepository : IStandardRegistryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StandardRegistryRepository> _logger;

        public StandardRegistryRepository(
            ApplicationDbContext context,
            ILogger<StandardRegistryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets standard registry entries with optional type filtering
        /// </summary>
        /// <param name="type">Optional registry type to filter by. If null, returns all registries.</param>
        /// <param name="showAll">If true, returns all entries including inactive ones. If false, returns only active entries.</param>
        /// <returns>List of standard registry entries matching the criteria</returns>
        public async Task<List<StandardRegistry>> GetByTypeAsync(string? type = null, bool showAll = false)
        {
            try
            {
                _logger.LogDebug("Querying standard registries with type: {Type}, showAll: {ShowAll}", type ?? "ALL", showAll);

                var query = _context.StandardRegistries.AsQueryable();

                // Filter by type if provided
                if (!string.IsNullOrWhiteSpace(type))
                {
                    query = query.Where(sr => sr.Type == type);
                }

                // Filter by IsActive if showAll is false
                if (!showAll)
                {
                    query = query.Where(sr => sr.IsActive);
                }

                var registries = await query
                    .OrderBy(sr => sr.Type)
                    .ThenBy(sr => sr.Code)
                    .ToListAsync();

                _logger.LogDebug("Found {Count} standard registries for type: {Type}", registries.Count, type ?? "ALL");

                return registries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error occurred while querying standard registries for type: {Type}", type ?? "ALL");
                throw;
            }
        }

        public async Task SetJsonDetailAsync(string code, string type, string jsonDetail)
        {
            var entity = await _context.StandardRegistries
                .FirstOrDefaultAsync(r => r.Code == code && r.Type == type && r.IsActive == true);
            if (entity == null)
                throw new KeyNotFoundException($"Registry entry '{code}' of type '{type}' not found.");

            entity.Json_detail = jsonDetail;
            entity.LastModifiedDate = VietnamDateTime.Now();
            await _context.SaveChangesAsync();
        }
    }
}
