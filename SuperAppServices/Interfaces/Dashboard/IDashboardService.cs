using SuperAppModels.DTOs;

namespace SuperAppServices.Interfaces.Dashboard
{
    /// <summary>Read-only aggregates for the SuperApp homepage progress dashboard (TungRoot #1481).</summary>
    public interface IDashboardService
    {
        /// <summary>Comment count per (week, project). Default range: last 26 weeks; default types devlog,comment,decision.</summary>
        Task<ResultOptions> GetActivityAsync(int userId, DateOnly? from, DateOnly? to, string? types);

        /// <summary>
        /// Habit trackers with their track/comment entries per day. Default range: last 56 days.
        /// taskIds → only those tasks; otherwise every repeat task. excludeTaskIds removes tasks in both cases.
        /// </summary>
        /// <param name="includeSensitive">Return tasks marked is_sensitive too (TOTP unlocked, #1489).</param>
        Task<ResultOptions> GetHabitsAsync(int userId, DateOnly? from, DateOnly? to, string? taskIds, string? excludeTaskIds, bool includeSensitive);
    }
}
