using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins.Dashboard;
using SuperAppModels.DTOs;
using SuperAppModels.Time;
using SuperAppServices.Interfaces.Dashboard;

namespace SuperAppServices.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private const int DefaultActivityDays = 26 * 7;
        private const int DefaultHabitDays = 56;

        private readonly IDashboardRepository _repository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(IDashboardRepository repository, ILogger<DashboardService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetActivityAsync(int userId, DateOnly? from, DateOnly? to, string? types)
        {
            try
            {
                var (start, end) = DashboardBuckets.ResolveRange(from, to, UserClock.UserToday(), DefaultActivityDays);
                var typeList = DashboardBuckets.ParseTypes(types, DashboardBuckets.DefaultActivityTypes);

                var projects = await _repository.GetProjectsAsync(userId);
                var rows = await _repository.GetActivityRowsAsync(
                    userId, UserClock.StartOfUserDayUtc(start), UserClock.StartOfUserDayUtc(end.AddDays(1)), typeList);

                var points = DashboardBuckets.BuildActivity(rows, projects, UserClock.ToUserDate);
                return Ok(points.Cast<object>().ToList(), $"Retrieved {points.Count} activity points ({start:yyyy-MM-dd} → {end:yyyy-MM-dd})");
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building dashboard activity for userId={UserId}", userId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> GetHabitsAsync(int userId, DateOnly? from, DateOnly? to, string? taskIds, string? excludeTaskIds)
        {
            try
            {
                var (start, end) = DashboardBuckets.ResolveRange(from, to, UserClock.UserToday(), DefaultHabitDays);
                var include = DashboardBuckets.ParseIds(taskIds);
                var exclude = DashboardBuckets.ParseIds(excludeTaskIds);

                var tasks = await _repository.GetTrackerTasksAsync(userId, include, exclude);
                var comments = tasks.Count == 0
                    ? new List<SuperAppModels.DTOs.Responses.Dashboard.DashboardCommentRow>()
                    : await _repository.GetTrackerCommentsAsync(
                        tasks.Select(t => t.TaskId).ToList(),
                        UserClock.StartOfUserDayUtc(start),
                        UserClock.StartOfUserDayUtc(end.AddDays(1)),
                        DashboardBuckets.HabitEntryTypes.ToList());

                var series = DashboardBuckets.BuildHabits(tasks, comments, UserClock.ToUserDate);
                return Ok(series.Cast<object>().ToList(), $"Retrieved {series.Count} trackers ({start:yyyy-MM-dd} → {end:yyyy-MM-dd})");
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building dashboard habits for userId={UserId}", userId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        private static ResultOptions Ok(List<object> data, string message)
            => new() { Success = true, Message = message, Data = data, TotalCount = data.Count, Status = 200 };

        private static ResultOptions BadRequest(string message)
            => new() { Success = false, Message = message, Status = 400 };
    }
}
