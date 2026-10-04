using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins.DailyLog;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.DailyLog;
using SuperAppModels.DTOs.Responses.DailyLog;

namespace SuperAppDataRepositories.Repositories.DailyLog
{
    public class DailyLogRepository : IDailyLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DailyLogRepository> _logger;

        public DailyLogRepository(ApplicationDbContext context, ILogger<DailyLogRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetLogsAsync(DailyLogFilterOptions filterOptions)
        {
            try
            {
                var query = _context.DailyLogs.AsNoTracking().Where(l => l.UserId == filterOptions.UserId);

                // LogDate is a calendar date (SQL date) — compared as DateOnly, no timezone involved.
                if (filterOptions.FromDate is DateOnly fromDate)
                    query = query.Where(l => l.LogDate >= fromDate);
                if (filterOptions.ToDate is DateOnly toDate)
                    query = query.Where(l => l.LogDate <= toDate);

                if (filterOptions.DeletedAt == "null")
                    query = query.Where(l => l.DeletedAt == null);
                else if (filterOptions.DeletedAt == "notNull")
                    query = query.Where(l => l.DeletedAt != null);
                else
                    query = query.Where(l => l.DeletedAt == null);

                var logs = await query.OrderByDescending(l => l.LogDate).ToListAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = "Daily logs retrieved successfully",
                    Data = logs.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily logs");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> GetLogByDateAsync(int userId, DateOnly logDate)
        {
            try
            {
                var log = await _context.DailyLogs.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == logDate && l.DeletedAt == null);

                return new ResultOptions
                {
                    Success = true,
                    Message = log != null ? "Daily log found" : "No daily log for this date",
                    Object = log,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily log by date");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertLogAsync(SuperAppModels.Models.DailyLog.DailyLog log)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var logDate = log.LogDate;
                    var existing = await _context.DailyLogs
                        .FirstOrDefaultAsync(l => l.UserId == log.UserId && l.LogDate == logDate && l.DeletedAt == null);

                    SuperAppModels.Models.DailyLog.DailyLog persisted;

                    if (existing != null)
                    {
                        existing.ValuesJson = log.ValuesJson;
                        // Only overwrite the template snapshot when the client actually sent one.
                        // Persistence should never blow away an existing snapshot with null.
                        if (log.TemplateJson != null) existing.TemplateJson = log.TemplateJson;
                        existing.DeletedAt = log.DeletedAt;
                        existing.UpdatedAt = DateTime.UtcNow;
                        persisted = existing;
                    }
                    else
                    {
                        log.CreatedAt = DateTime.UtcNow;
                        log.UpdatedAt = DateTime.UtcNow;
                        log.DeletedAt = null;
                        _context.DailyLogs.Add(log);
                        persisted = log;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ResultOptions
                    {
                        Success = true,
                        Message = existing != null ? "Daily log updated" : "Daily log created",
                        Object = persisted,
                        Status = 200
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error upserting daily log");
                    return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
                }
            });
        }

        public async Task<List<DailyLogHistoryPoint>> GetFieldHistoryAsync(int userId, string fieldKey, DateOnly? from, DateOnly? to)
        {
            // fieldKey has format "<section>.<field_key>", e.g. "input.general"
            // Use JSON_VALUE server-side so we only ship (date, value) pairs, not full blobs.
            var jsonPath = "$.\"" + fieldKey.Replace("\"", "") + "\"";

            var query = _context.DailyLogs.AsNoTracking()
                .Where(l => l.UserId == userId && l.DeletedAt == null);

            if (from is DateOnly fromDate) query = query.Where(l => l.LogDate >= fromDate);
            if (to is DateOnly toDate) query = query.Where(l => l.LogDate <= toDate);

            var rows = await query
                .OrderBy(l => l.LogDate)
                .Select(l => new { l.LogDate, l.ValuesJson })
                .ToListAsync();

            var result = new List<DailyLogHistoryPoint>(rows.Count);
            foreach (var row in rows)
            {
                var value = ExtractJsonField(row.ValuesJson, fieldKey);
                if (value != null)
                    result.Add(new DailyLogHistoryPoint { LogDate = row.LogDate, Value = value });
            }
            return result;
        }

        private static string? ExtractJsonField(string valuesJson, string fieldKey)
        {
            if (string.IsNullOrEmpty(valuesJson)) return null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(valuesJson);
                if (!doc.RootElement.TryGetProperty(fieldKey, out var el)) return null;
                return el.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => el.GetString(),
                    System.Text.Json.JsonValueKind.Number => el.GetRawText(),
                    System.Text.Json.JsonValueKind.True => "true",
                    System.Text.Json.JsonValueKind.False => "false",
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => el.GetRawText(),
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
