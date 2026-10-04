using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins.DailyLog;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.DailyLog;
using SuperAppServices.Interfaces.DailyLog;

namespace SuperAppServices.Services.DailyLog
{
    public class DailyLogService : IDailyLogService
    {
        private readonly IDailyLogRepository _repository;
        private readonly ILogger<DailyLogService> _logger;

        public DailyLogService(IDailyLogRepository repository, ILogger<DailyLogService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ResultOptions> GetLogsAsync(DailyLogFilterOptions filterOptions)
            => _repository.GetLogsAsync(filterOptions);

        public Task<ResultOptions> GetLogByDateAsync(int userId, DateOnly logDate)
            => _repository.GetLogByDateAsync(userId, logDate);

        public async Task<ResultOptions> UpsertLogAsync(UpsertDailyLogRequest request)
        {
            try
            {
                if (request.DeletedAt.HasValue && request.LogDate == default)
                    return new ResultOptions { Success = false, Message = "logDate is required", Status = 400 };

                var log = new SuperAppModels.Models.DailyLog.DailyLog
                {
                    UserId = request.UserId,
                    LogDate = request.LogDate,
                    ValuesJson = string.IsNullOrWhiteSpace(request.ValuesJson) ? "{}" : request.ValuesJson,
                    TemplateJson = string.IsNullOrWhiteSpace(request.TemplateJson) ? null : request.TemplateJson,
                    DeletedAt = request.DeletedAt
                };

                return await _repository.UpsertLogAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting daily log");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> GetFieldHistoryAsync(int userId, string fieldKey, DateOnly? from, DateOnly? to)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fieldKey) || !fieldKey.Contains('.'))
                    return new ResultOptions { Success = false, Message = "fieldKey must be in format 'section.field_key'", Status = 400 };

                var points = await _repository.GetFieldHistoryAsync(userId, fieldKey, from, to);
                return new ResultOptions
                {
                    Success = true,
                    Message = $"Retrieved {points.Count} history points",
                    Data = points.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily log history");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
