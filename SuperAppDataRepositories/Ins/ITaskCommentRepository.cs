using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface ITaskCommentRepository
    {
        Task<ResultOptions> GetCommentsByTaskIdAsync(int taskId, int userId);
        Task<ResultOptions> UpsertCommentAsync(TaskComment comment);
        Task<ResultOptions> DeleteCommentAsync(int commentId, int userId);
    }
}
