using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for task comment operations
    /// </summary>
    public class TaskCommentService : ITaskCommentService
    {
        private readonly ITaskCommentRepository _repository;
        private readonly ILogger<TaskCommentService> _logger;

        public TaskCommentService(
            ITaskCommentRepository repository,
            ILogger<TaskCommentService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetCommentsByTaskIdAsync(int taskId, int userId)
        {
            try
            {
                _logger.LogInformation("Getting comments for taskId: {TaskId}", taskId);
                return await _repository.GetCommentsByTaskIdAsync(taskId, userId);
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

                var comment = new TaskComment
                {
                    Id = request.Id,
                    TaskId = request.TaskId,
                    ParentCommentId = request.ParentCommentId,
                    Content = request.Content,
                    UserId = userId,
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
