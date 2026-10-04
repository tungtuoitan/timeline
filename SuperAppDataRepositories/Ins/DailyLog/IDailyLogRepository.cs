using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.DailyLog;
using SuperAppModels.DTOs.Responses.DailyLog;
using SuperAppModels.Models.DailyLog;

namespace SuperAppDataRepositories.Ins.DailyLog
{
    public interface IDailyLogRepository
    {
        Task<ResultOptions> GetLogsAsync(DailyLogFilterOptions filterOptions);
        Task<ResultOptions> GetLogByDateAsync(int userId, DateOnly logDate);
        Task<ResultOptions> UpsertLogAsync(SuperAppModels.Models.DailyLog.DailyLog log);
        Task<List<DailyLogHistoryPoint>> GetFieldHistoryAsync(int userId, string fieldKey, DateOnly? from, DateOnly? to);
    }

    public interface IDailyLogTemplateRepository
    {
        Task<ResultOptions> GetTemplateAsync(int userId);
        Task<ResultOptions> UpsertTemplateAsync(List<DailyLogFieldTemplate> fields);
    }
}
