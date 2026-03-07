using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    public interface ILifeLogService
    {
        Task<ResultOptions> GetTracksAsync(LifeLogTrackFilterOptions filterOptions);
        Task<ResultOptions> UpsertTracksAsync(List<UpsertLifeLogTrackRequest> requests);
        Task<ResultOptions> GetLogsAsync(LifeLogLogFilterOptions filterOptions);
        Task<ResultOptions> UpsertLogsAsync(List<UpsertLifeLogLogRequest> requests);
    }
}
