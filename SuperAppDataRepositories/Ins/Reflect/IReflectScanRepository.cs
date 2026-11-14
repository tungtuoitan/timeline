using SuperAppModels.DTOs.Reflect;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Ins.Reflect
{
    public interface IReflectScanRepository
    {
        /// <summary>Users who have at least one row the scan has not read since its last edit</summary>
        Task<List<int>> GetUserIdsWithPendingAsync();
        /// <summary>The user's timezone id from the profile (null when none is saved)</summary>
        Task<string?> GetTimezoneIdAsync(int userId);
        /// <summary>Rows never scanned, or edited after their last scan, oldest first</summary>
        Task<List<ReflectEntry>> GetPendingEntriesAsync(int userId, int take);
        Task<List<ReflectMetric>> GetMetricsAsync(int userId);
        /// <summary>`readAt` is when the rows were loaded, so an edit made while the model was working still counts as pending. Stores what the scan found: tag + hash on each row, its old numbers replaced by the new ones, new metrics created on first use</summary>
        Task SaveOutcomesAsync(int userId, List<ReflectScanOutcome> outcomes, DateTime readAt);
        /// <summary>Marks rows as read without a model call (their text did not change, or there is no text)</summary>
        Task MarkScannedAsync(int userId, Dictionary<string, string> hashByEntryId, DateTime readAt);
        Task<List<ReflectSeries>> GetSeriesAsync(int userId, DateTime? from, DateTime? to);
    }
}
