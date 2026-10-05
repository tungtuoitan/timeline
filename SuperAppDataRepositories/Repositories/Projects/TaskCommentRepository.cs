using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for task comment data access
    /// </summary>
    public class TaskCommentRepository : ITaskCommentRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskCommentRepository> _logger;

        public TaskCommentRepository(
            ApplicationDbContext context,
            ILogger<TaskCommentRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all non-deleted comments for a task, ordered by creation date ASC.
        /// Validates task belongs to user via project ownership.
        /// </summary>
        public async Task<ResultOptions> GetCommentsByTaskIdAsync(int taskId, int userId, List<string>? types = null)
        {
            try
            {
                _logger.LogInformation("Getting comments for taskId: {TaskId}, userId: {UserId}", taskId, userId);

                // Validate task belongs to user via project
                var userProjectIds = await _context.Projects
                    .Where(p => p.UserId == userId)
                    .Select(p => p.Id)
                    .ToListAsync();

                var taskExists = await _context.ProTasks
                    .AnyAsync(t => t.Id == taskId && userProjectIds.Contains(t.ProjectId));

                if (!taskExists)
                {
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Task not found or access denied",
                        Status = 404
                    };
                }

                var query = _context.TaskComments
                    .AsNoTracking()
                    .Where(c => c.TaskId == taskId && c.DeletedAt == null);
                if (types?.Count > 0)
                    query = query.Where(c => types.Contains(c.Type));

                // Order by when it happened (occurred_at), falling back to created_at
                var comments = await query
                    .OrderBy(c => c.OccurredAt ?? c.CreatedAt)
                    .ThenBy(c => c.Id)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} comments for taskId: {TaskId}", comments.Count, taskId);

                return new ResultOptions
                {
                    Success = true,
                    Message = "Comments retrieved successfully",
                    Data = comments.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for taskId: {TaskId}", taskId);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Create or update a comment. Id == 0 → create, Id > 0 → update.
        /// </summary>
        public async Task<ResultOptions> UpsertCommentAsync(TaskComment comment)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Captured before SaveChanges assigns the new Id
                    var isNew = comment.Id == 0;
                    if (isNew)
                    {
                        // CREATE
                        _logger.LogInformation("Creating comment for taskId: {TaskId}", comment.TaskId);
                        comment.CreatedAt = DateTime.UtcNow;
                        comment.UpdatedAt = DateTime.UtcNow;
                        _context.TaskComments.Add(comment);
                    }
                    else
                    {
                        // UPDATE
                        _logger.LogInformation("Updating comment ID: {CommentId}", comment.Id);
                        var existing = await _context.TaskComments.FindAsync(comment.Id);

                        if (existing == null || existing.DeletedAt != null)
                        {
                            await transaction.RollbackAsync();
                            return new ResultOptions
                            {
                                Success = false,
                                Message = "Comment not found",
                                Status = 404
                            };
                        }

                        existing.Content = comment.Content;
                        if (!string.IsNullOrEmpty(comment.Type)) existing.Type = comment.Type;
                        if (comment.OccurredAt.HasValue) existing.OccurredAt = comment.OccurredAt;
                        existing.UpdatedAt = DateTime.UtcNow;
                        comment = existing;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Comment upsert successful, ID: {CommentId}", comment.Id);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = isNew ? "Comment created" : "Comment updated",
                        Data = new List<object> { comment },
                        Status = 200
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error upserting comment");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = ex.Message + " - rolled back",
                        Status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Soft delete a comment and its replies.
        /// </summary>
        public async Task<ResultOptions> DeleteCommentAsync(int commentId, int userId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Soft deleting comment ID: {CommentId}", commentId);

                    var comment = await _context.TaskComments.FindAsync(commentId);

                    if (comment == null || comment.DeletedAt != null)
                    {
                        await transaction.RollbackAsync();
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "Comment not found",
                            Status = 404
                        };
                    }

                    // Validate ownership
                    if (comment.UserId != userId)
                    {
                        await transaction.RollbackAsync();
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "Not authorized to delete this comment",
                            Status = 403
                        };
                    }

                    var now = DateTime.UtcNow;

                    // Soft delete the comment
                    comment.DeletedAt = now;
                    comment.UpdatedAt = now;

                    // Cascade soft delete replies
                    var replies = await _context.TaskComments
                        .Where(c => c.ParentCommentId == commentId && c.DeletedAt == null)
                        .ToListAsync();

                    foreach (var reply in replies)
                    {
                        reply.DeletedAt = now;
                        reply.UpdatedAt = now;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Soft deleted comment ID: {CommentId} and {ReplyCount} replies",
                        commentId, replies.Count);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Comment deleted",
                        Status = 200
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting comment ID: {CommentId}", commentId);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = ex.Message + " - rolled back",
                        Status = 500
                    };
                }
            });
        }
    }
}
