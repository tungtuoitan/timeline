using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins.Reflect;
using SuperAppModels.DTOs;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Repositories.Reflect
{
    public class ReflectRepository : IReflectRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReflectRepository> _logger;

        /// <summary>"I think" may only be changed this long after the row was created (the point of the journal is that it cannot be rewritten later); a few seconds of grace cover clock differences.</summary>
        private static readonly TimeSpan ThinkEditWindow = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(5);

        public ReflectRepository(ApplicationDbContext context, ILogger<ReflectRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetEntriesAsync(int userId)
        {
            try
            {
                var entries = await _context.ReflectEntries.AsNoTracking()
                    .Where(r => r.UserId == userId && r.DeletedAt == null)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = "Reflect entries retrieved successfully",
                    Data = entries.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reflect entries");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertEntryAsync(ReflectEntry entry)
        {
            try
            {
                var existing = await _context.ReflectEntries.FirstOrDefaultAsync(r => r.Id == entry.Id);

                // An id that belongs to another user is never touched
                if (existing != null && existing.UserId != entry.UserId)
                    return new ResultOptions { Success = false, Message = "Reflect entry not found", Status = 404 };

                if (existing == null)
                {
                    entry.CreatedAt = DateTime.UtcNow;
                    entry.UpdatedAt = entry.CreatedAt;
                    _context.ReflectEntries.Add(entry);
                    existing = entry;
                }
                else
                {
                    if (existing.DeletedAt == null && existing.Think != entry.Think && DateTime.UtcNow - existing.CreatedAt > ThinkEditWindow)
                        return new ResultOptions { Success = false, Message = "I think is locked 3 minutes after the row was created", Status = 409 };

                    existing.Type = entry.Type;
                    existing.Think = entry.Think;
                    existing.Real = entry.Real;
                    existing.DeletedAt = null;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return new ResultOptions { Success = true, Message = "Reflect entry saved", Object = existing, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving reflect entry");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> DeleteEntryAsync(int userId, string id)
        {
            try
            {
                var existing = await _context.ReflectEntries
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && r.DeletedAt == null);
                if (existing == null)
                    return new ResultOptions { Success = true, Message = "Reflect entry already gone", Status = 200 };

                existing.DeletedAt = DateTime.UtcNow;
                existing.UpdatedAt = existing.DeletedAt.Value;
                await _context.SaveChangesAsync();
                return new ResultOptions { Success = true, Message = "Reflect entry deleted", Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reflect entry");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
