using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Reflect;

namespace SuperAppServices.Interfaces.Reflect
{
    public interface IReflectService
    {
        Task<ResultOptions> GetEntriesAsync(int userId);
        Task<ResultOptions> UpsertEntryAsync(UpsertReflectEntryRequest request);
        Task<ResultOptions> DeleteEntryAsync(int userId, string id);
    }
}
