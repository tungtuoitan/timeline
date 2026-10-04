using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Data;
using SuperAppModels.DTOs;

namespace SuperAppServices.Services.Projects
{
    /// <summary>
    /// Ownership checks for writes to projects, tasks and task comments.
    /// A task belongs to the owner of its project (pro.task has no user_id).
    /// Every write path must call these before any side effect.
    /// </summary>
    public class OwnershipGuard
    {
        private readonly ApplicationDbContext _context;

        public OwnershipGuard(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Same response as the read paths: don't reveal whether the row exists.
        /// </summary>
        public static ResultOptions Denied(string what) => new ResultOptions
        {
            Success = false,
            Message = $"{what} not found or access denied",
            Status = 404
        };

        public async Task<bool> ProjectsOwnedAsync(int userId, IEnumerable<int> projectIds)
        {
            var ids = projectIds.Distinct().ToList();
            if (ids.Count == 0) return true;
            var owned = await _context.Projects.CountAsync(p => ids.Contains(p.Id) && p.UserId == userId);
            return owned == ids.Count;
        }

        public async Task<bool> TasksOwnedAsync(int userId, IEnumerable<int> taskIds)
        {
            var ids = taskIds.Distinct().ToList();
            if (ids.Count == 0) return true;
            var owned = await _context.ProTasks
                .Where(t => ids.Contains(t.Id))
                .Join(_context.Projects, t => t.ProjectId, p => p.Id, (t, p) => p.UserId)
                .CountAsync(ownerId => ownerId == userId);
            return owned == ids.Count;
        }

        public async Task<bool> WorkspacesOwnedAsync(int userId, IEnumerable<int> workspaceIds)
        {
            var ids = workspaceIds.Distinct().ToList();
            if (ids.Count == 0) return true;
            var owned = await _context.Workspaces.CountAsync(w => ids.Contains(w.Id) && w.UserId == userId);
            return owned == ids.Count;
        }

        public async Task<bool> WorkspaceItemsOwnedAsync(int userId, IEnumerable<int> workspaceItemIds)
        {
            var ids = workspaceItemIds.Distinct().ToList();
            if (ids.Count == 0) return true;
            var owned = await _context.WorkspaceItems
                .Where(i => ids.Contains(i.Id))
                .Join(_context.Workspaces, i => i.WorkspaceId, w => w.Id, (i, w) => w.UserId)
                .CountAsync(ownerId => ownerId == userId);
            return owned == ids.Count;
        }

        /// <summary>
        /// Comment must exist on the given task and be written by the user.
        /// </summary>
        public Task<bool> CommentOwnedAsync(int userId, int commentId, int taskId) =>
            _context.TaskComments.AnyAsync(c => c.Id == commentId && c.TaskId == taskId && c.UserId == userId);

        /// <summary>
        /// Reply target must be a comment on the same task.
        /// </summary>
        public Task<bool> CommentOnTaskAsync(int commentId, int taskId) =>
            _context.TaskComments.AnyAsync(c => c.Id == commentId && c.TaskId == taskId);
    }
}
