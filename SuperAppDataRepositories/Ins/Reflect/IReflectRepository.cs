using SuperAppModels.DTOs;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Ins.Reflect
{
    public interface IReflectRepository
    {
        Task<ResultOptions> GetEntriesAsync(int userId, DateTime? createdFrom, DateTime? createdTo);
        Task<ResultOptions> UpsertEntryAsync(ReflectEntry entry);
        Task<ResultOptions> DeleteEntryAsync(int userId, string id);
        Task<List<SuperAppModels.DTOs.Reflect.ReflectSeries>> GetSeriesAsync(int userId, DateTime? from, DateTime? to);
    }
}
