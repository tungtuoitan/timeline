using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services.Projects
{
    /// <summary>
    /// Service for task comment operations
    /// </summary>
    public class TaskCommentService : ITaskCommentService
    {
        private readonly ITaskCommentRepository _repository;
        private readonly ILogger<TaskCommentService> _logger;

        private readonly OwnershipGuard _ownership;

        public TaskCommentService(
            ITaskCommentRepository repository,
            ILogger<TaskCommentService> logger,
            OwnershipGuard ownership)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ownership = ownership ?? throw new ArgumentNullException(nameof(ownership));
        }

        public async Task<ResultOptions> GetCommentsByTaskIdAsync(int taskId, int userId, string? type = null)
        {
            try
            {
                _logger.LogInformation("Getting comments for taskId: {TaskId}, type: {Type}", taskId, type);
                List<string>? types = null;
                if (!string.IsNullOrWhiteSpace(type))
                {
                    types = type.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
                    var unknown = types.Where(t => !TaskCommentTypes.All.Contains(t)).ToList();
                    if (unknown.Any())
                        return new ResultOptions { Success = false, Message = $"Unknown comment type(s): {string.Join(", ", unknown)}", Status = 400 };
                }
                return await _repository.GetCommentsByTaskIdAsync(taskId, userId, types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for taskId: {TaskId}", taskId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertCommentAsync(UpsertTaskCommentRequest request, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return new ResultOptions { Success = false, Message = "Comment content is required", Status = 400 };
                }

                _logger.LogInformation("Upserting comment for taskId: {TaskId}, commentId: {CommentId}",
                    request.TaskId, request.Id);

                // Ownership: task must belong to the user; an edit must target the user's own
                // comment on that task; a reply must point at a comment on the same task.
                if (!await _ownership.TasksOwnedAsync(userId, new[] { request.TaskId }))
                    return OwnershipGuard.Denied("Task");
                if (request.Id > 0 && !await _ownership.CommentOwnedAsync(userId, request.Id, request.TaskId))
                    return OwnershipGuard.Denied("Comment");
                if (request.ParentCommentId.HasValue && !await _ownership.CommentOnTaskAsync(request.ParentCommentId.Value, request.TaskId))
                    return OwnershipGuard.Denied("Parent comment");

                if (request.Type != null && !TaskCommentTypes.All.Contains(request.Type))
                    return new ResultOptions { Success = false, Message = $"Unknown comment type: {request.Type}", Status = 400 };

                var comment = new TaskComment
                {
                    Id = request.Id,
                    TaskId = request.TaskId,
                    ParentCommentId = request.ParentCommentId,
                    Content = request.Content,
                    UserId = userId,
                    // Update: empty Type = keep existing (repository); create: default "comment"
                    Type = request.Type ?? (request.Id == 0 ? TaskCommentTypes.Comment : string.Empty),
                    OccurredAt = request.OccurredAt,
                };

                return await _repository.UpsertCommentAsync(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting comment");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> DeleteCommentAsync(int commentId, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting comment ID: {CommentId}", commentId);
                return await _repository.DeleteCommentAsync(commentId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment ID: {CommentId}", commentId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
