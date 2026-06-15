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
        Task CheckAllUsersAsync(IKViewerTracker? viewerTracker = null);
        Task PushAllUsersAsync(IKViewerTracker? viewerTracker = null);
        Task<ResultOptions> ResetConflictAndRetryAsync(int userId);

        /// <summary>
        /// Force-overwrites the remote repo with DB content. Ignores remote state entirely:
        /// writes all DB files, commits, and force-pushes. Use when remote is out of sync
        /// and normal push hits structural conflicts.
        /// </summary>
        Task<ResultOptions> ForceUpdateRemoteAsync(int userId);

        /// <summary>
        /// Same as <see cref="ForceUpdateRemoteAsync(int)"/> but cancellable. Used by the
        /// background daemon so new DB events can interrupt an in-progress push at well-defined
        /// checkpoints (mid-libgit2sharp call cannot be cancelled — the next debounce will retry).
        /// </summary>
        Task<ResultOptions> ForceUpdateRemoteAsync(int userId, CancellationToken ct);

        /// <summary>
        /// Compares current remote repo state vs DB without modifying anything.
        /// Fetches remote first to get latest state.
        /// </summary>
        Task<KRepoCompareDiffResponse> GetCompareDiffAsync(int userId);

        /// <summary>
        /// Applies a per-entity resolution choice (keep_db / keep_repo) for each
        /// conflict, then force-pushes the resulting DB state to remote.
        /// </summary>
        Task<ResultOptions> ResolveConflictsAsync(int userId, List<KRepoResolveConflictItem> items);
    }
}
