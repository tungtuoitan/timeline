using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins.DailyLog;
using SuperAppModels.DTOs;
using SuperAppModels.Models.DailyLog;

namespace SuperAppDataRepositories.Repositories.DailyLog
{
    public class DailyLogTemplateRepository : IDailyLogTemplateRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DailyLogTemplateRepository> _logger;

        public DailyLogTemplateRepository(ApplicationDbContext context, ILogger<DailyLogTemplateRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetTemplateAsync(int userId)
        {
            try
            {
                var fields = await _context.DailyLogFieldTemplates.AsNoTracking()
                    .Where(f => f.UserId == userId && f.DeletedAt == null)
                    .OrderBy(f => f.Section).ThenBy(f => f.SortOrder).ThenBy(f => f.Id)
                    .ToListAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = "Template retrieved",
                    Data = fields.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily log template");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertTemplateAsync(List<DailyLogFieldTemplate> fields)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (fields == null || fields.Count == 0)
                    {
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "No template fields provided",
                            Status = 400
                        };
                    }

                    var idsToUpdate = fields.Where(f => f.Id > 0).Select(f => f.Id).ToList();
                    var existingDict = await _context.DailyLogFieldTemplates
                        .Where(f => idsToUpdate.Contains(f.Id))
                        .ToDictionaryAsync(f => f.Id, f => f);

                    var upserted = new List<DailyLogFieldTemplate>();

                    foreach (var field in fields)
                    {
                        if (field.Id > 0 && existingDict.TryGetValue(field.Id, out var existing))
                        {
                            existing.Section = field.Section;
                            existing.FieldKey = field.FieldKey;
                            existing.Label = field.Label;
                            existing.FieldType = field.FieldType;
                            existing.RangeMin = field.RangeMin;
                            existing.RangeMax = field.RangeMax;
                            existing.SortOrder = field.SortOrder;
                            existing.GroupOrder = field.GroupOrder;
                            existing.GroupLabel = field.GroupLabel;
                            existing.LineOrder = field.LineOrder;
                            existing.DeletedAt = field.DeletedAt;
                            existing.UpdatedAt = DateTime.UtcNow;
                            upserted.Add(existing);
                        }
                        else
                        {
                            field.CreatedAt = DateTime.UtcNow;
                            field.UpdatedAt = DateTime.UtcNow;
                            _context.DailyLogFieldTemplates.Add(field);
                            upserted.Add(field);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Upserted {upserted.Count} template fields",
                        Data = upserted.Cast<object>().ToList(),
                        Status = 200
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error upserting daily log template");
                    return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
                }
            });
        }
    }
}
