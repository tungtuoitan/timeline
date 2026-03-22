using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    public interface ITaskCommentService
    {
        Task<ResultOptions> GetCommentsByTaskIdAsync(int taskId, int userId);
        Task<ResultOptions> UpsertCommentAsync(UpsertTaskCommentRequest request, int userId);
        Task<ResultOptions> DeleteCommentAsync(int commentId, int userId);
    }
}
