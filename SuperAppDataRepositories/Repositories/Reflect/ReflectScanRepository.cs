using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins.Reflect;
using SuperAppModels.DTOs.Reflect;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Repositories.Reflect
{
    public class ReflectScanRepository : IReflectScanRepository
    {
        private readonly ApplicationDbContext _context;

        public ReflectScanRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private IQueryable<ReflectEntry> Pending(int? userId) =>
            _context.ReflectEntries.Where(r => r.DeletedAt == null && (userId == null || r.UserId == userId)
                && (r.ScannedAt == null || r.UpdatedAt > r.ScannedAt));

        public Task<List<int>> GetUserIdsWithPendingAsync() => Pending(null).Select(r => r.UserId).Distinct().ToListAsync();

        public Task<string?> GetTimezoneIdAsync(int userId) =>
            _context.UserProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => p.Timezone).FirstOrDefaultAsync();

        public Task<List<ReflectEntry>> GetPendingEntriesAsync(int userId, int take) =>
            Pending(userId).OrderBy(r => r.CreatedAt).Take(take).ToListAsync();

        public Task<List<ReflectMetric>> GetMetricsAsync(int userId) =>
            _context.ReflectMetrics.AsNoTracking().Where(m => m.UserId == userId && m.DeletedAt == null).OrderBy(m => m.Code).ToListAsync();

        public async Task SaveOutcomesAsync(int userId, List<ReflectScanOutcome> outcomes, DateTime readAt)
        {
            var ids = outcomes.Select(o => o.EntryId).ToList();
            var entries = await _context.ReflectEntries.Where(r => r.UserId == userId && ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id);
            var metrics = await _context.ReflectMetrics.Where(m => m.UserId == userId).ToDictionaryAsync(m => m.Code);
            var old = await _context.ReflectObservations.Where(o => o.UserId == userId && ids.Contains(o.EntryId)).ToListAsync();
            _context.ReflectObservations.RemoveRange(old);

            foreach (var outcome in outcomes)
            {
                if (!entries.TryGetValue(outcome.EntryId, out var entry)) continue;
                entry.Tag = outcome.Tag;
                entry.ScannedHash = outcome.Hash;
                entry.ScannedAt = readAt;

                // One number per metric and day in a row: the last one the model gave wins
                var perKey = new Dictionary<(string, DateTime), ReflectObservationDraft>();
                foreach (var d in outcome.Observations) perKey[(d.MetricCode, d.OccurredOn.Date)] = d;

                foreach (var d in perKey.Values)
                {
                    if (!metrics.TryGetValue(d.MetricCode, out var metric))
                    {
                        metric = new ReflectMetric { UserId = userId, Code = d.MetricCode, Label = d.Label, Unit = d.Unit, Aggregate = d.Aggregate };
                        _context.ReflectMetrics.Add(metric);
                        metrics[d.MetricCode] = metric;
                    }
                    else if (metric.DeletedAt != null) metric.DeletedAt = null;
                    _context.ReflectObservations.Add(new ReflectObservation
                    {
                        UserId = userId, EntryId = entry.Id, Metric = metric, OccurredOn = d.OccurredOn.Date, Value = d.Value, Quote = d.Quote
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task MarkScannedAsync(int userId, Dictionary<string, string> hashByEntryId, DateTime readAt)
        {
            var ids = hashByEntryId.Keys.ToList();
            var entries = await _context.ReflectEntries.Where(r => r.UserId == userId && ids.Contains(r.Id)).ToListAsync();
            foreach (var e in entries) { e.ScannedHash = hashByEntryId[e.Id]; e.ScannedAt = readAt; }
            await _context.SaveChangesAsync();
        }

        public async Task<List<ReflectSeries>> GetSeriesAsync(int userId, DateTime? from, DateTime? to)
        {
            var rows = _context.ReflectObservations.AsNoTracking().Where(o => o.UserId == userId
                && _context.ReflectEntries.Any(e => e.Id == o.EntryId && e.DeletedAt == null));
            if (from.HasValue) rows = rows.Where(o => o.OccurredOn >= from.Value.Date);
            if (to.HasValue) rows = rows.Where(o => o.OccurredOn <= to.Value.Date);
            var data = await rows.Select(o => new { o.MetricId, o.OccurredOn, o.Value }).ToListAsync();
            var metrics = await _context.ReflectMetrics.AsNoTracking().Where(m => m.UserId == userId && m.DeletedAt == null).ToListAsync();

            var result = new List<ReflectSeries>();
            foreach (var m in metrics)
            {
                var points = data.Where(d => d.MetricId == m.Id).GroupBy(d => d.OccurredOn.Date).OrderBy(g => g.Key)
                    .Select(g => new ReflectSeriesPoint
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Value = m.Aggregate == "avg" ? g.Average(x => x.Value) : m.Aggregate == "max" ? g.Max(x => x.Value) : g.Sum(x => x.Value)
                    }).ToList();
                if (points.Count > 0) result.Add(new ReflectSeries { Code = m.Code, Label = m.Label, Unit = m.Unit, Aggregate = m.Aggregate, Points = points });
            }
            return result;
        }
    }
}
