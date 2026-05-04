using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppDataRepositories.Ins
{
    public interface ILifeLogRepository
    {
        Task<ResultOptions> GetTracksAsync(LifeLogTrackFilterOptions filterOptions);
        Task<ResultOptions> UpsertTracksAsync(List<SuperAppModels.Models.LifeLogTrack> tracks);
        Task<ResultOptions> GetLogsAsync(LifeLogLogFilterOptions filterOptions);
        Task<ResultOptions> UpsertLogsAsync(List<SuperAppModels.Models.LifeLogLog> logs);
    }
}
