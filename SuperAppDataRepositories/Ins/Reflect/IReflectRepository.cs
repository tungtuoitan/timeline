using SuperAppModels.DTOs;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Ins.Reflect
{
    public interface IReflectRepository
    {
        Task<ResultOptions> GetEntriesAsync(int userId, DateTime? createdFrom, DateTime? createdTo);
        Task<ResultOptions> UpsertEntryAsync(ReflectEntry entry);
        Task<ResultOptions> DeleteEntryAsync(int userId, string id);
    }
}
