using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface ITaskRepository
    {
        Task<ResultOptions> GetTasksAsync(TaskFilterOptions filterOptions);
        Task<ResultOptions> UpsertTasksAsync(List<ProTask> tasks);
    }
}
