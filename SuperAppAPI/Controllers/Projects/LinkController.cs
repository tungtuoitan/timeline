using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.Projects
{
    /// <summary>
    /// Links of tasks/projects + rename/edit of workspace files (task #1477).
    /// A link is a file (mimeType text/x-uri) in the project's workspace.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize]
    public class LinkController : BaseAuthController
    {
        private readonly ILinkService _linkService;

        public LinkController(ILinkService linkService)
        {
            _linkService = linkService ?? throw new ArgumentNullException(nameof(linkService));
        }

        /// <summary>Links of a task: items linked via task_workspace_item + links in the task folder.</summary>
        [HttpGet("task/{taskId:int}/links")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public Task<IActionResult> GetTaskLinks(int taskId) =>
            Run(userId => _linkService.GetTaskLinksAsync(taskId, userId));

        /// <summary>Add a URL (created in the task folder) or link an existing item of the project's workspace.</summary>
        [HttpPost("task/{taskId:int}/links")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status400BadRequest)]
        public Task<IActionResult> AddTaskLink(int taskId, [FromBody] AddLinkRequest request) =>
            Run(userId => _linkService.AddTaskLinkAsync(taskId, request, userId, GetUserEmail()));

        /// <summary>Unlink; a link that lives in the task folder is soft-deleted too.</summary>
        [HttpDelete("task/{taskId:int}/links/{workspaceItemId:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public Task<IActionResult> RemoveTaskLink(int taskId, int workspaceItemId) =>
            Run(userId => _linkService.RemoveTaskLinkAsync(taskId, workspaceItemId, userId, GetUserEmail()));

        /// <summary>Links of a project: links in the "Links" folder at the root of its workspace.</summary>
        [HttpGet("project/{projectId:int}/links")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public Task<IActionResult> GetProjectLinks(int projectId) =>
            Run(userId => _linkService.GetProjectLinksAsync(projectId, userId));

        [HttpPost("project/{projectId:int}/links")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status400BadRequest)]
        public Task<IActionResult> AddProjectLink(int projectId, [FromBody] AddLinkRequest request) =>
            Run(userId => _linkService.AddProjectLinkAsync(projectId, request, userId, GetUserEmail()));

        [HttpDelete("project/{projectId:int}/links/{workspaceItemId:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public Task<IActionResult> RemoveProjectLink(int projectId, int workspaceItemId) =>
            Run(userId => _linkService.RemoveProjectLinkAsync(projectId, workspaceItemId, userId, GetUserEmail()));

        /// <summary>Rename a workspace file; change the url of a link.</summary>
        [HttpPatch("file/{fileId:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status400BadRequest)]
        public Task<IActionResult> UpdateFile(int fileId, [FromBody] UpdateFileRequest request) =>
            Run(userId => _linkService.UpdateFileAsync(fileId, request, userId));

        private async Task<IActionResult> Run(Func<int, Task<ResultOptions>> action)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token");

            var result = await action(userId.Value);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }
    }
}
