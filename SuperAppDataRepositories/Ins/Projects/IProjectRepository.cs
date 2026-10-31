using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IProjectRepository
    {
        Task<ResultOptions> GetProjectsAsync(ProjectFilterOptions filterOptions);
        Task<ResultOptions> UpsertProjectsAsync(List<Project> projects);

        /// <summary>Partial update: only non-null fields are applied, ClearFields null out the listed fields.</summary>
        Task<ResultOptions> PatchProjectAsync(int projectId, PatchProjectRequest request);
    }
}
