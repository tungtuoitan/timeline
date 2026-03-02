using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface ITaskWorkspaceItemRepository
    {
        Task<ResultOptions> GetByTaskIdAsync(int taskId);
        Task<ResultOptions> CreateAsync(TaskWorkspaceItem item);
        Task<ResultOptions> DeleteAsync(int id);
    }
}
