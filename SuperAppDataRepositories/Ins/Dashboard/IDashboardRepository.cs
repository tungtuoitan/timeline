using SuperAppModels.DTOs.Responses.Dashboard;

namespace SuperAppDataRepositories.Ins.Dashboard
{
    /// <summary>Read-only raw rows for the progress dashboard (TungRoot #1481). Aggregation lives in the service.</summary>
    public interface IDashboardRepository
    {
        /// <summary>Non-deleted projects of the user.</summary>
        Task<List<DashboardProjectRow>> GetProjectsAsync(int userId);

        /// <summary>
        /// Comments (without content) on the user's non-deleted tasks whose instant occurredAt ?? createdAt
        /// is in [fromUtc, toUtcExclusive) and whose type is in <paramref name="types"/>.
        /// </summary>
        Task<List<DashboardCommentRow>> GetActivityRowsAsync(int userId, DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<string> types);

        /// <summary>
        /// Tracker tasks of the user. taskIds given → exactly those (owned, non-deleted) tasks;
        /// otherwise every non-deleted task with type = "repeat". excludeTaskIds is applied in both cases.
        /// </summary>
        Task<List<DashboardTaskRow>> GetTrackerTasksAsync(int userId, IReadOnlyCollection<int>? taskIds, IReadOnlyCollection<int>? excludeTaskIds);

        /// <summary>Comments (with content) of the given tasks in [fromUtc, toUtcExclusive), filtered by type.</summary>
        Task<List<DashboardCommentRow>> GetTrackerCommentsAsync(IReadOnlyCollection<int> taskIds, DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<string> types);
    }
}
