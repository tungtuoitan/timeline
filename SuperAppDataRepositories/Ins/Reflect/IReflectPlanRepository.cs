using SuperAppModels.DTOs;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Ins.Reflect
{
    public interface IReflectPlanRepository
    {
        Task<ResultOptions> GetItemsAsync(int userId, DateTime? from, DateTime? to);
        Task<ResultOptions> UpsertItemAsync(ReflectPlanItem item);
        Task<ResultOptions> DeleteItemAsync(int userId, string id);
    }
}
