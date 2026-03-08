using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for project operations
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// Get all projects with optional filters
        /// </summary>
        /// <param name="filterOptions">Filter options</param>
        /// <returns>ResultOptions containing list of projects</returns>
        Task<ResultOptions> GetProjectsAsync(ProjectFilterOptions filterOptions);

        Task<ResultOptions> GetProjectByIdAsync(int id, int userId);

        /// <summary>
        /// Batch create or update multiple projects (upsert)
        /// </summary>
        /// <param name="requests">List of project upsert requests</param>
        /// <returns>ResultOptions containing batch operation results</returns>
        Task<ResultOptions> UpsertProjectsAsync(List<UpsertProjectRequest> requests);
    }
}
