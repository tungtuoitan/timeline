using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IKRepoSyncService
    {
        Task<KRepoSyncStatusResponse> GetStatusAsync(int userId);
        Task<ResultOptions> SaveConfigAsync(int userId, string repoUrl, string branch, string pat);
        Task<ResultOptions> PushToRepoAsync(int userId);
        Task<ResultOptions> PullFromRepoAsync(int userId);
        Task<KRepoSyncDiffResponse> GetDiffAsync(int userId);
        Task CheckAndUpdateStatusAsync(int userId);
        Task CheckAllUsersAsync();
        Task PushAllUsersAsync();
        Task<ResultOptions> ResetConflictAndRetryAsync(int userId);
    }
}
