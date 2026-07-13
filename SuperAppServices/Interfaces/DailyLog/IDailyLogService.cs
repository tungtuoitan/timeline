using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.DailyLog;

namespace SuperAppServices.Interfaces.DailyLog
{
    public interface IDailyLogService
    {
        Task<ResultOptions> GetLogsAsync(DailyLogFilterOptions filterOptions);
        Task<ResultOptions> GetLogByDateAsync(int userId, DateTime logDate);
        Task<ResultOptions> UpsertLogAsync(UpsertDailyLogRequest request);
        Task<ResultOptions> GetFieldHistoryAsync(int userId, string fieldKey, DateTime? from, DateTime? to);
    }

    public interface IDailyLogTemplateService
    {
        /// <summary>
        /// Load the user's template. Auto-seeds the default template on first access.
        /// </summary>
        Task<ResultOptions> GetTemplateAsync(int userId);
        Task<ResultOptions> UpsertTemplateAsync(int userId, List<UpsertDailyLogTemplateRequest> requests);
    }
}
