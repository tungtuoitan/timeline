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

        /// <summary>
        /// Force-overwrites the remote repo with DB content. Ignores remote state entirely:
        /// writes all DB files, commits, and force-pushes. Use when remote is out of sync
        /// and normal push hits structural conflicts.
        /// </summary>
        Task<ResultOptions> ForceUpdateRemoteAsync(int userId);

        /// <summary>
        /// Compares current remote repo state vs DB without modifying anything.
        /// Fetches remote first to get latest state.
        /// </summary>
        Task<KRepoCompareDiffResponse> GetCompareDiffAsync(int userId);
    }
}
