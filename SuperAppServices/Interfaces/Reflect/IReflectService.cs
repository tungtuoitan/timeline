using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Reflect;

namespace SuperAppServices.Interfaces.Reflect
{
    public interface IReflectService
    {
        /// <summary>Rows of the user; createdFrom / createdTo are UTC instants (inclusive), null = no bound</summary>
        Task<ResultOptions> GetEntriesAsync(int userId, DateTime? createdFrom, DateTime? createdTo);
        Task<ResultOptions> UpsertEntryAsync(UpsertReflectEntryRequest request);
        Task<ResultOptions> DeleteEntryAsync(int userId, string id);
    }
}
