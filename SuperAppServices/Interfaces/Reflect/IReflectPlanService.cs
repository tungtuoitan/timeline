using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Reflect;

namespace SuperAppServices.Interfaces.Reflect
{
    public interface IReflectPlanService
    {
        /// <summary>Planned tasks of the user between two calendar days (inclusive), null = no bound</summary>
        Task<ResultOptions> GetItemsAsync(int userId, DateTime? from, DateTime? to);
        Task<ResultOptions> UpsertItemAsync(UpsertReflectPlanItemRequest request);
        Task<ResultOptions> DeleteItemAsync(int userId, string id);
    }
}
