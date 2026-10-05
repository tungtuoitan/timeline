using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins.Finance;
using SuperAppModels.DTOs.Responses.Finance;
using SuperAppModels.Models.Finance;

namespace SuperAppDataRepositories.Repositories.Finance
{
    public class FinanceRepository : IFinanceRepository
    {
        private readonly ApplicationDbContext _context;

        public FinanceRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<List<FinTransaction>> GetTransactionsAsync(int userId, DateTime? fromUtc, DateTime? toUtc,
            string? account, string? kind, bool uncategorizedOnly, int limit)
        {
            var query = _context.FinTransactions.AsNoTracking()
                .Where(t => t.UserId == userId && t.DeletedAt == null);
            if (fromUtc is DateTime f) query = query.Where(t => t.OccurredAt >= f);
            if (toUtc is DateTime e) query = query.Where(t => t.OccurredAt < e);
            if (!string.IsNullOrEmpty(account)) query = query.Where(t => t.Account == account);
            if (!string.IsNullOrEmpty(kind)) query = query.Where(t => t.Kind == kind);
            if (uncategorizedOnly)
                query = query.Where(t => t.Category == null && (t.Kind == FinKinds.Expense || t.Kind == FinKinds.Income));
            return query.OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.Id).Take(limit).ToListAsync();
        }

        public Task<List<FinTransaction>> GetLedgerAsync(int userId, DateTime toUtc)
            => _context.FinTransactions.AsNoTracking()
                .Where(t => t.UserId == userId && t.DeletedAt == null && t.OccurredAt < toUtc)
                .OrderBy(t => t.OccurredAt).ThenBy(t => t.Id)
                .Select(t => new FinTransaction
                {
                    Id = t.Id,
                    Account = t.Account,
                    OccurredAt = t.OccurredAt,
                    Asset = t.Asset,
                    Amount = t.Amount,
                    ValueVnd = t.ValueVnd,
                    Kind = t.Kind,
                    Category = t.Category,
                })
                .ToListAsync();

        public Task<FinTransaction?> GetByIdAsync(int userId, int id)
            => _context.FinTransactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.DeletedAt == null);

        public async Task<FinUpsertResultDto> UpsertTransactionsAsync(int userId, List<FinTransaction> rows)
        {
            var result = new FinUpsertResultDto();
            var keyed = rows.Where(r => r.ExternalId != null).ToList();
            var existing = new Dictionary<(string, string), FinTransaction>();
            foreach (var bySource in keyed.GroupBy(r => r.Source))
            {
                var ids = bySource.Select(r => r.ExternalId!).Distinct().ToList();
                // chunk to stay far below SQL Server's 2100 parameter limit
                foreach (var chunk in ids.Chunk(1000))
                {
                    var found = await _context.FinTransactions
                        .Where(t => t.UserId == userId && t.DeletedAt == null && t.Source == bySource.Key && chunk.Contains(t.ExternalId!))
                        .ToListAsync();
                    foreach (var t in found) existing[(t.Source, t.ExternalId!)] = t;
                }
            }

            var now = DateTime.UtcNow;
            foreach (var row in rows)
            {
                if (row.ExternalId != null && existing.TryGetValue((row.Source, row.ExternalId), out var cur))
                {
                    var changed = cur.Account != row.Account || cur.OccurredAt != row.OccurredAt || cur.Asset != row.Asset
                        || cur.Amount != row.Amount || cur.ValueVnd != row.ValueVnd || cur.Description != row.Description
                        || cur.Counterparty != row.Counterparty || cur.RawJson != row.RawJson;
                    if (!changed) { result.Unchanged++; continue; }
                    cur.Account = row.Account;
                    cur.OccurredAt = row.OccurredAt;
                    cur.Asset = row.Asset;
                    cur.Amount = row.Amount;
                    cur.ValueVnd = row.ValueVnd;
                    cur.Description = row.Description;
                    cur.Counterparty = row.Counterparty;
                    cur.RawJson = row.RawJson;
                    cur.UpdatedAt = now;
                    result.Updated++;
                    continue;
                }

                row.UserId = userId;
                row.CreatedAt = now;
                row.UpdatedAt = now;
                _context.FinTransactions.Add(row);
                if (row.ExternalId != null) existing[(row.Source, row.ExternalId)] = row; // duplicate inside the batch
                result.Inserted++;
            }

            await _context.SaveChangesAsync();
            return result;
        }

        public async Task SaveAsync(FinTransaction row)
        {
            row.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(int userId, int id)
        {
            var row = await GetByIdAsync(userId, id);
            if (row == null) return false;
            row.DeletedAt = DateTime.UtcNow;
            row.UpdatedAt = row.DeletedAt.Value;
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<List<FinPrice>> GetPricesAsync(IReadOnlyCollection<string> assets, DateOnly to)
            => _context.FinPrices.AsNoTracking()
                .Where(p => assets.Contains(p.Asset) && p.Date <= to)
                .ToListAsync();

        public async Task<int> UpsertPricesAsync(List<FinPrice> prices)
        {
            var now = DateTime.UtcNow;
            foreach (var group in prices.GroupBy(p => (p.Asset, p.Quote)))
            {
                var dates = group.Select(p => p.Date).Distinct().ToList();
                var found = new Dictionary<DateOnly, FinPrice>();
                foreach (var chunk in dates.Chunk(1000))
                {
                    var rows = await _context.FinPrices
                        .Where(p => p.Asset == group.Key.Asset && p.Quote == group.Key.Quote && chunk.Contains(p.Date))
                        .ToListAsync();
                    foreach (var r in rows) found[r.Date] = r;
                }
                foreach (var p in group)
                {
                    if (found.TryGetValue(p.Date, out var cur))
                    {
                        cur.Price = p.Price;
                        cur.Source = p.Source;
                        cur.FetchedAt = now;
                    }
                    else
                    {
                        p.FetchedAt = now;
                        _context.FinPrices.Add(p);
                        found[p.Date] = p;
                    }
                }
            }
            await _context.SaveChangesAsync();
            return prices.Count;
        }

        public Task<List<FinPriceLatestDto>> GetLatestPriceDatesAsync()
            => _context.FinPrices.AsNoTracking()
                .GroupBy(p => new { p.Asset, p.Quote })
                .Select(g => new FinPriceLatestDto { Asset = g.Key.Asset, Quote = g.Key.Quote, Date = g.Max(p => p.Date) })
                .OrderBy(x => x.Asset).ThenBy(x => x.Quote)
                .ToListAsync();
    }
}
