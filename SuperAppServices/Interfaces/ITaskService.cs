using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for task operations
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Get all tasks with optional filters
        /// </summary>
        /// <param name="filterOptions">Filter options including projectIds</param>
        /// <returns>ResultOptions containing list of tasks (raw list, no tree)</returns>
        Task<ResultOptions> GetTasksAsync(TaskFilterOptions filterOptions);

        Task<ResultOptions> GetTaskByIdAsync(int id, int userId);

        /// <summary>
        /// Batch create or update multiple tasks (upsert)
        /// </summary>
        /// <param name="requests">List of task upsert requests</param>
        /// <returns>ResultOptions containing batch operation results</returns>
        Task<ResultOptions> UpsertTasksAsync(List<UpsertTaskRequest> requests, int userId);

        /// <summary>
        /// Partial update a single task — only non-null fields in request are updated.
        /// </summary>
        Task<ResultOptions> PatchTaskAsync(int taskId, PatchTaskRequest request, int userId);
    }
}
