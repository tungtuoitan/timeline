using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IProjectRepository
    {
        Task<ResultOptions> GetProjectsAsync(ProjectFilterOptions filterOptions);
        Task<ResultOptions> UpsertProjectsAsync(List<Project> projects);
    }
}
