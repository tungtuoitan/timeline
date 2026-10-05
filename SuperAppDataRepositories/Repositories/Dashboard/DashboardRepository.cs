using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins.Dashboard;
using SuperAppModels.DTOs.Responses.Dashboard;

namespace SuperAppDataRepositories.Repositories.Dashboard
{
    public class DashboardRepository : IDashboardRepository
    {
        private const string RepeatTaskType = "repeat";

        private readonly ApplicationDbContext _context;

        public DashboardRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<List<DashboardProjectRow>> GetProjectsAsync(int userId)
            => _context.Projects.AsNoTracking()
                .Where(p => p.UserId == userId && p.DeletedAt == null)
                .Select(p => new DashboardProjectRow { ProjectId = p.Id, Name = p.Name, Status = p.Status })
                .ToListAsync();

        public Task<List<DashboardCommentRow>> GetActivityRowsAsync(int userId, DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<string> types)
            => (from c in _context.TaskComments.AsNoTracking()
                join t in _context.ProTasks.AsNoTracking() on c.TaskId equals t.Id
                join p in _context.Projects.AsNoTracking() on t.ProjectId equals p.Id
                where p.UserId == userId && p.DeletedAt == null && t.DeletedAt == null && c.DeletedAt == null
                      && types.Contains(c.Type)
                      && (c.OccurredAt ?? c.CreatedAt) >= fromUtc
                      && (c.OccurredAt ?? c.CreatedAt) < toUtcExclusive
                select new DashboardCommentRow
                {
                    CommentId = c.Id,
                    TaskId = c.TaskId,
                    ProjectId = t.ProjectId,
                    Type = c.Type,
                    At = c.OccurredAt ?? c.CreatedAt
                })
                .ToListAsync();

        public Task<List<DashboardTaskRow>> GetTrackerTasksAsync(int userId, IReadOnlyCollection<int>? taskIds, IReadOnlyCollection<int>? excludeTaskIds)
        {
            var query = from t in _context.ProTasks.AsNoTracking()
                        join p in _context.Projects.AsNoTracking() on t.ProjectId equals p.Id
                        where p.UserId == userId && p.DeletedAt == null && t.DeletedAt == null
                        select t;

            query = taskIds?.Count > 0
                ? query.Where(t => taskIds.Contains(t.Id))
                : query.Where(t => t.Type == RepeatTaskType);

            if (excludeTaskIds?.Count > 0)
                query = query.Where(t => !excludeTaskIds.Contains(t.Id));

            return query
                .OrderBy(t => t.Id)
                .Select(t => new DashboardTaskRow { TaskId = t.Id, Title = t.Title, ProjectId = t.ProjectId, Status = t.Status })
                .ToListAsync();
        }

        public Task<List<DashboardCommentRow>> GetTrackerCommentsAsync(IReadOnlyCollection<int> taskIds, DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<string> types)
            => _context.TaskComments.AsNoTracking()
                .Where(c => taskIds.Contains(c.TaskId) && c.DeletedAt == null
                            && types.Contains(c.Type)
                            && (c.OccurredAt ?? c.CreatedAt) >= fromUtc
                            && (c.OccurredAt ?? c.CreatedAt) < toUtcExclusive)
                .OrderBy(c => c.OccurredAt ?? c.CreatedAt)
                .Select(c => new DashboardCommentRow
                {
                    CommentId = c.Id,
                    TaskId = c.TaskId,
                    Type = c.Type,
                    Content = c.Content,
                    At = c.OccurredAt ?? c.CreatedAt
                })
                .ToListAsync();
    }
}
