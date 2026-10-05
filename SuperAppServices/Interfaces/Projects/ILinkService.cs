using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Links of tasks/projects (task #1477). A link is a dbo.files row (mime text/x-uri) shown in
    /// the project's workspace: task links live in the task folder and are tied to the task through
    /// pro.task_workspace_item; project links live in the "Links" folder at the workspace root.
    /// </summary>
    public interface ILinkService
    {
        Task<ResultOptions> GetTaskLinksAsync(int taskId, int userId);
        Task<ResultOptions> AddTaskLinkAsync(int taskId, AddLinkRequest request, int userId, string? userEmail);
        Task<ResultOptions> RemoveTaskLinkAsync(int taskId, int workspaceItemId, int userId, string? userEmail);
        /// <summary>Task folder id, created when missing (a new note of the task goes there).</summary>
        Task<ResultOptions> GetOrCreateTaskFolderAsync(int taskId, int userId, string? userEmail);

        Task<ResultOptions> GetProjectLinksAsync(int projectId, int userId);
        Task<ResultOptions> AddProjectLinkAsync(int projectId, AddLinkRequest request, int userId, string? userEmail);
        Task<ResultOptions> RemoveProjectLinkAsync(int projectId, int workspaceItemId, int userId, string? userEmail);

        /// <summary>Rename a file; change the url of a link.</summary>
        Task<ResultOptions> UpdateFileAsync(int fileId, UpdateFileRequest request, int userId);
    }
}
