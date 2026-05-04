using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface ITaskRepository
    {
        Task<ResultOptions> GetTasksAsync(TaskFilterOptions filterOptions);
        Task<ResultOptions> UpsertTasksAsync(List<ProTask> tasks);

        /// <summary>
        /// Partial update: load existing task by ID, merge only non-null fields, save.
        /// Returns the updated task wrapped in ResultOptions.
        /// </summary>
        Task<ResultOptions> PatchTaskAsync(int taskId, PatchTaskRequest request);
    }
}
