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

        public async Task<ResultOptions> GetEntriesAsync(int userId, DateTime? createdFrom, DateTime? createdTo)
        {
            try
            {
                // CreatedAt is stored in UTC, and both bounds arrive as UTC instants
                var query = _context.ReflectEntries.AsNoTracking()
                    .Where(r => r.UserId == userId && r.DeletedAt == null);
                if (createdFrom.HasValue) query = query.Where(r => r.CreatedAt >= createdFrom.Value);
                if (createdTo.HasValue) query = query.Where(r => r.CreatedAt <= createdTo.Value);

                var entries = await query.OrderBy(r => r.CreatedAt).ToListAsync();

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

        /// <summary>Every point read out of the user's rows, grouped per metric (oldest first); the page decides how to draw each one</summary>
        public async Task<List<SuperAppModels.DTOs.Reflect.ReflectSeries>> GetSeriesAsync(int userId, DateTime? from, DateTime? to)
        {
            var rows = _context.ReflectObservations.AsNoTracking().Where(o => o.UserId == userId
                && _context.ReflectEntries.Any(e => e.Id == o.EntryId && e.DeletedAt == null));
            if (from.HasValue) rows = rows.Where(o => o.OccurredOn >= from.Value.Date);
            if (to.HasValue) rows = rows.Where(o => o.OccurredOn <= to.Value.Date);
            var data = await rows.OrderBy(o => o.OccurredOn).ThenBy(o => o.Id).ToListAsync();
            var metrics = await _context.ReflectMetrics.AsNoTracking().Where(m => m.UserId == userId && m.DeletedAt == null).OrderBy(m => m.Code).ToListAsync();

            var result = new List<SuperAppModels.DTOs.Reflect.ReflectSeries>();
            foreach (var m in metrics)
            {
                var points = data.Where(d => d.MetricId == m.Id).Select(d => new SuperAppModels.DTOs.Reflect.ReflectSeriesPoint
                {
                    Date = d.OccurredOn.ToString("yyyy-MM-dd"), Value = d.Value, Text = d.ValueText, Item = d.Item, Quote = d.Quote
                }).ToList();
                if (points.Count > 0)
                    result.Add(new SuperAppModels.DTOs.Reflect.ReflectSeries { Code = m.Code, Label = m.Label, Unit = m.Unit, Aggregate = m.Aggregate, Shape = m.Shape, Sensitive = m.Sensitive, Points = points });
            }
            return result;
        }
    }
}
