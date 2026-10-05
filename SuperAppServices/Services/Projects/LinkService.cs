using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Helpers;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services.Projects
{
    /// <summary>
    /// Links of tasks/projects (task #1477) on top of the workspace: items are created/soft-deleted
    /// through IWorkspaceItemService (same path ids + keyword sync as the explorer), task ↔ item
    /// through pro.task_workspace_item.
    /// </summary>
    public class LinkService : ILinkService
    {
        private readonly ApplicationDbContext _context;
        private readonly OwnershipGuard _ownership;
        private readonly IWorkspaceItemService _workspaceItemService;
        private readonly IWorkspaceService _workspaceService;
        private readonly ILogger<LinkService> _logger;

        public LinkService(
            ApplicationDbContext context,
            OwnershipGuard ownership,
            IWorkspaceItemService workspaceItemService,
            IWorkspaceService workspaceService,
            ILogger<LinkService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _ownership = ownership ?? throw new ArgumentNullException(nameof(ownership));
            _workspaceItemService = workspaceItemService ?? throw new ArgumentNullException(nameof(workspaceItemService));
            _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ================================================================ task links

        /// <summary>
        /// Items linked through task_workspace_item (any type) + links sitting directly in the task folder.
        /// </summary>
        public async Task<ResultOptions> GetTaskLinksAsync(int taskId, int userId)
        {
            if (!await _ownership.TasksOwnedAsync(userId, new[] { taskId }))
                return OwnershipGuard.Denied("Task");

            var task = await _context.ProTasks.AsNoTracking().FirstAsync(t => t.Id == taskId);
            var links = await LoadTaskLinksAsync(task);
            return Ok(links, "Task links retrieved");
        }

        public async Task<ResultOptions> AddTaskLinkAsync(int taskId, AddLinkRequest request, int userId, string? userEmail)
        {
            if (!await _ownership.TasksOwnedAsync(userId, new[] { taskId }))
                return OwnershipGuard.Denied("Task");

            var task = await _context.ProTasks.FirstAsync(t => t.Id == taskId);
            var workspaceId = await _context.Projects
                .Where(p => p.Id == task.ProjectId)
                .Select(p => p.WorkspaceId)
                .FirstOrDefaultAsync();
            if (!workspaceId.HasValue)
                return Fail(400, "Project of this task has no workspace");

            int workspaceItemId;
            if (request.WorkspaceItemId.HasValue)
            {
                // Link an existing item of the same workspace (note, file, folder, link)
                var item = await _context.WorkspaceItems.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == request.WorkspaceItemId.Value && i.DeletedAt == null);
                if (item == null || item.WorkspaceId != workspaceId.Value)
                    return Fail(400, "Workspace item not found in this project's workspace");
                workspaceItemId = item.Id;
            }
            else
            {
                if (!LinkRules.IsValidUrl(request.Url))
                    return Fail(400, "Url must be an absolute http/https URL");

                var folderId = await EnsureTaskFolderAsync(task, workspaceId.Value, userId, userEmail);
                if (folderId == null)
                    return Fail(500, "Could not create the task folder");

                var created = await CreateLinkItemAsync(workspaceId.Value, folderId.Value, request, userId, userEmail);
                if (created.Error != null)
                    return created.Error;
                workspaceItemId = created.Id;
            }

            var exists = await _context.Set<TaskWorkspaceItem>()
                .AnyAsync(x => x.TaskId == taskId && x.WorkspaceItemId == workspaceItemId);
            if (!exists)
            {
                var entityType = await _context.WorkspaceItems
                    .Where(i => i.Id == workspaceItemId).Select(i => i.EntityType).FirstAsync();
                _context.Set<TaskWorkspaceItem>().Add(new TaskWorkspaceItem
                {
                    TaskId = taskId,
                    WorkspaceItemId = workspaceItemId,
                    ItemType = entityType
                });
                await _context.SaveChangesAsync();
            }

            _workspaceService.InvalidateTreeCache(workspaceId.Value, userId);
            var links = await LoadTaskLinksAsync(task);
            var added = links.FirstOrDefault(l => l.WorkspaceItemId == workspaceItemId);
            _logger.LogInformation("Task {TaskId}: linked workspace item {ItemId}", taskId, workspaceItemId);
            return Ok(added != null ? new List<LinkDto> { added } : new List<LinkDto>(), "Link added");
        }

        /// <summary>
        /// Unlink. A link (url) that lives in the task folder belongs to the task → soft-delete it too;
        /// any other item is only unlinked.
        /// </summary>
        public async Task<ResultOptions> RemoveTaskLinkAsync(int taskId, int workspaceItemId, int userId, string? userEmail)
        {
            if (!await _ownership.TasksOwnedAsync(userId, new[] { taskId }))
                return OwnershipGuard.Denied("Task");

            var task = await _context.ProTasks.AsNoTracking().FirstAsync(t => t.Id == taskId);
            var rows = await _context.Set<TaskWorkspaceItem>()
                .Where(x => x.TaskId == taskId && x.WorkspaceItemId == workspaceItemId)
                .ToListAsync();
            var item = await _context.WorkspaceItems.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == workspaceItemId && i.DeletedAt == null);

            var inTaskFolder = item != null && task.FolderWorkspaceItemId.HasValue
                && item.ParentId == task.FolderWorkspaceItemId.Value;
            var ownedLink = inTaskFolder && await IsLinkItemAsync(item!);

            if (rows.Count == 0 && !ownedLink)
                return Fail(404, "Link not found on this task");

            if (rows.Count > 0)
            {
                _context.Set<TaskWorkspaceItem>().RemoveRange(rows);
                await _context.SaveChangesAsync();
            }

            if (ownedLink)
            {
                var deleted = await SoftDeleteItemAsync(item!, userId, userEmail);
                if (deleted != null)
                    return deleted;
            }
            else if (item != null)
            {
                _workspaceService.InvalidateTreeCache(item.WorkspaceId, userId);
            }

            return new ResultOptions { Success = true, Message = "Link removed", Status = 200 };
        }

        // ============================================================= project links

        public async Task<ResultOptions> GetProjectLinksAsync(int projectId, int userId)
        {
            if (!await _ownership.ProjectsOwnedAsync(userId, new[] { projectId }))
                return OwnershipGuard.Denied("Project");

            var workspaceId = await ProjectWorkspaceIdAsync(projectId);
            if (!workspaceId.HasValue)
                return Ok(new List<LinkDto>(), "Project has no workspace");

            var folderId = await FindProjectFolderAsync(workspaceId.Value);
            if (!folderId.HasValue)
                return Ok(new List<LinkDto>(), "No links yet");

            var items = await _context.WorkspaceItems.AsNoTracking()
                .Where(i => i.ParentId == folderId.Value && i.DeletedAt == null)
                .ToListAsync();
            var links = (await ToDtosAsync(items, null)).Where(l => l.IsLink)
                .OrderBy(l => l.CreatedAt).ToList();
            return Ok(links, "Project links retrieved");
        }

        public async Task<ResultOptions> AddProjectLinkAsync(int projectId, AddLinkRequest request, int userId, string? userEmail)
        {
            if (!await _ownership.ProjectsOwnedAsync(userId, new[] { projectId }))
                return OwnershipGuard.Denied("Project");
            if (!LinkRules.IsValidUrl(request.Url))
                return Fail(400, "Url must be an absolute http/https URL");

            var workspaceId = await ProjectWorkspaceIdAsync(projectId);
            if (!workspaceId.HasValue)
                return Fail(400, "Project has no workspace");

            var folderId = await FindProjectFolderAsync(workspaceId.Value)
                ?? await CreateFolderAsync(workspaceId.Value, null, LinkRules.ProjectFolderName, userId, userEmail);
            if (!folderId.HasValue)
                return Fail(500, "Could not create the Links folder");

            var created = await CreateLinkItemAsync(workspaceId.Value, folderId.Value, request, userId, userEmail);
            if (created.Error != null)
                return created.Error;

            _workspaceService.InvalidateTreeCache(workspaceId.Value, userId);
            var item = await _context.WorkspaceItems.AsNoTracking().FirstAsync(i => i.Id == created.Id);
            return Ok(await ToDtosAsync(new List<WorkspaceItemEntity> { item }, null), "Link added");
        }

        public async Task<ResultOptions> RemoveProjectLinkAsync(int projectId, int workspaceItemId, int userId, string? userEmail)
        {
            if (!await _ownership.ProjectsOwnedAsync(userId, new[] { projectId }))
                return OwnershipGuard.Denied("Project");

            var workspaceId = await ProjectWorkspaceIdAsync(projectId);
            var folderId = workspaceId.HasValue ? await FindProjectFolderAsync(workspaceId.Value) : null;
            var item = folderId.HasValue
                ? await _context.WorkspaceItems.AsNoTracking().FirstOrDefaultAsync(i =>
                    i.Id == workspaceItemId && i.ParentId == folderId.Value && i.DeletedAt == null)
                : null;
            if (item == null || !await IsLinkItemAsync(item))
                return Fail(404, "Link not found on this project");

            var deleted = await SoftDeleteItemAsync(item, userId, userEmail);
            return deleted ?? new ResultOptions { Success = true, Message = "Link removed", Status = 200 };
        }

        // ===================================================================== files

        public async Task<ResultOptions> UpdateFileAsync(int fileId, UpdateFileRequest request, int userId)
        {
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId && f.DeletedAt == null);
            if (file == null)
                return OwnershipGuard.Denied("File");

            if (request.Name == null && request.Url == null)
                return Fail(400, "Nothing to update");

            if (request.Name != null)
            {
                var name = request.Name.Trim();
                if (name.Length == 0 || name.Length > LinkRules.MaxNameLength)
                    return Fail(400, $"Name must be between 1 and {LinkRules.MaxNameLength} characters");
                file.Name = name;
            }

            if (request.Url != null)
            {
                if (!LinkRules.IsLink(file.MimeType))
                    return Fail(400, "Only a link can change its url");
                if (!LinkRules.IsValidUrl(request.Url))
                    return Fail(400, "Url must be an absolute http/https URL");
                file.Url = request.Url.Trim();
            }

            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var workspaceIds = await _context.WorkspaceItems
                .Where(i => i.EntityType == LinkRules.EntityTypeFile && i.EntityId == fileId)
                .Select(i => i.WorkspaceId).Distinct().ToListAsync();
            foreach (var wsId in workspaceIds)
                _workspaceService.InvalidateTreeCache(wsId, userId);

            return new ResultOptions
            {
                Success = true,
                Message = "File updated",
                Status = 200,
                Object = new FileData
                {
                    Id = file.Id, UserId = file.UserId, Name = file.Name, Url = file.Url, FileSize = file.FileSize,
                    MimeType = file.MimeType, Extension = file.Extension, StatusCode = file.StatusCode,
                    CreatedAt = file.CreatedAt ?? DateTime.UtcNow, UpdatedAt = file.UpdatedAt, DeletedAt = file.DeletedAt
                }
            };
        }

        // =================================================================== helpers

        private async Task<List<LinkDto>> LoadTaskLinksAsync(ProTask task)
        {
            var linkedRows = await _context.Set<TaskWorkspaceItem>().AsNoTracking()
                .Where(x => x.TaskId == task.Id)
                .ToListAsync();
            var linkedIds = linkedRows.Select(x => x.WorkspaceItemId).ToList();

            var items = await _context.WorkspaceItems.AsNoTracking()
                .Where(i => i.DeletedAt == null &&
                    (linkedIds.Contains(i.Id) ||
                     (task.FolderWorkspaceItemId.HasValue && i.ParentId == task.FolderWorkspaceItemId.Value
                      && i.EntityType == LinkRules.EntityTypeFile)))
                .ToListAsync();

            var rowByItem = linkedRows.GroupBy(x => x.WorkspaceItemId).ToDictionary(g => g.Key, g => g.First().Id);
            var dtos = await ToDtosAsync(items, rowByItem);
            // Folder children are listed only when they are links (plain files stay in the Inner List)
            return dtos.Where(d => d.TaskWorkspaceItemId.HasValue || d.IsLink)
                .OrderBy(d => d.CreatedAt).ThenBy(d => d.WorkspaceItemId).ToList();
        }

        private async Task<List<LinkDto>> ToDtosAsync(List<WorkspaceItemEntity> items, Dictionary<int, int>? rowByItem)
        {
            var fileIds = items.Where(i => i.EntityType == LinkRules.EntityTypeFile).Select(i => i.EntityId).ToList();
            var noteIds = items.Where(i => i.EntityType == LinkRules.EntityTypeNote).Select(i => i.EntityId).ToList();
            var folderIds = items.Where(i => i.EntityType == LinkRules.EntityTypeFolder).Select(i => i.EntityId).ToList();

            var files = await _context.Files.AsNoTracking().Where(f => fileIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id);
            var notes = await _context.Notes.AsNoTracking().Where(n => noteIds.Contains(n.Id))
                .Select(n => new { n.Id, n.Name }).ToDictionaryAsync(n => n.Id, n => n.Name);
            var folders = await _context.Folders.AsNoTracking().Where(f => folderIds.Contains(f.Id))
                .Select(f => new { f.Id, f.Name }).ToDictionaryAsync(f => f.Id, f => f.Name);

            var result = new List<LinkDto>();
            foreach (var i in items)
            {
                var dto = new LinkDto
                {
                    WorkspaceItemId = i.Id, WorkspaceId = i.WorkspaceId, ParentId = i.ParentId,
                    EntityType = i.EntityType, EntityId = i.EntityId, CreatedAt = i.CreatedAt,
                    TaskWorkspaceItemId = rowByItem != null && rowByItem.TryGetValue(i.Id, out var rowId) ? rowId : null
                };
                switch (i.EntityType)
                {
                    case LinkRules.EntityTypeFile:
                        if (!files.TryGetValue(i.EntityId, out var f) || f.DeletedAt != null) continue;
                        dto.Name = f.Name; dto.Url = f.Url; dto.MimeType = f.MimeType; dto.IsLink = LinkRules.IsLink(f.MimeType);
                        break;
                    case LinkRules.EntityTypeNote:
                        if (!notes.TryGetValue(i.EntityId, out var noteName)) continue;
                        dto.Name = noteName;
                        break;
                    case LinkRules.EntityTypeFolder:
                        if (!folders.TryGetValue(i.EntityId, out var folderName)) continue;
                        dto.Name = folderName;
                        break;
                    default:
                        continue;
                }
                result.Add(dto);
            }
            return result;
        }

        private async Task<bool> IsLinkItemAsync(WorkspaceItemEntity item) =>
            item.EntityType == LinkRules.EntityTypeFile
            && await _context.Files.AnyAsync(f => f.Id == item.EntityId && f.MimeType == LinkRules.MimeType);

        private Task<int?> ProjectWorkspaceIdAsync(int projectId) =>
            _context.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync();

        /// <summary>Root folder named "Links" (not deleted) of the workspace.</summary>
        private async Task<int?> FindProjectFolderAsync(int workspaceId)
        {
            return await _context.WorkspaceItems.AsNoTracking()
                .Where(i => i.WorkspaceId == workspaceId && i.ParentId == null
                            && i.EntityType == LinkRules.EntityTypeFolder && i.DeletedAt == null)
                .Join(_context.Folders.Where(f => f.Name == LinkRules.ProjectFolderName && f.DeletedAt == null),
                    i => i.EntityId, f => f.Id, (i, f) => (int?)i.Id)
                .OrderBy(id => id)
                .FirstOrDefaultAsync();
        }

        /// <summary>Task folder (task.FolderWorkspaceItemId) — created like the FE does when missing or deleted.</summary>
        private async Task<int?> EnsureTaskFolderAsync(ProTask task, int workspaceId, int userId, string? userEmail)
        {
            if (task.FolderWorkspaceItemId.HasValue)
            {
                var alive = await _context.WorkspaceItems.AnyAsync(i =>
                    i.Id == task.FolderWorkspaceItemId.Value && i.WorkspaceId == workspaceId && i.DeletedAt == null);
                if (alive)
                    return task.FolderWorkspaceItemId.Value;
            }

            var folderId = await CreateFolderAsync(workspaceId, null, task.Title, userId, userEmail);
            if (!folderId.HasValue)
                return null;

            task.FolderWorkspaceItemId = folderId.Value;
            await _context.SaveChangesAsync();
            return folderId;
        }

        private async Task<int?> CreateFolderAsync(int workspaceId, int? parentId, string name, int userId, string? userEmail)
        {
            var request = new UpsertWorkspaceItemRequest
            {
                Action = WorkspaceItemAction.Create,
                WorkspaceId = workspaceId,
                UserId = userId,
                CreatedBy = userEmail,
                ParentId = parentId,
                EntityType = LinkRules.EntityTypeFolder,
                FolderData = new UpsertFolderData { Name = string.IsNullOrWhiteSpace(name) ? "Untitled" : name }
            };
            var result = await _workspaceItemService.UpsertWorkspaceItemsAsync(new List<UpsertWorkspaceItemRequest> { request }, userId, workspaceId);
            if (!result.Success)
            {
                _logger.LogWarning("Create folder '{Name}' in workspace {WorkspaceId} failed: {Message}", name, workspaceId, result.Message);
                return null;
            }
            return CreatedItemId(result);
        }

        private async Task<(int Id, ResultOptions? Error)> CreateLinkItemAsync(
            int workspaceId, int parentId, AddLinkRequest request, int userId, string? userEmail)
        {
            var url = request.Url!.Trim();
            var item = new UpsertWorkspaceItemRequest
            {
                Action = WorkspaceItemAction.Create,
                WorkspaceId = workspaceId,
                UserId = userId,
                CreatedBy = userEmail,
                ParentId = parentId,
                EntityType = LinkRules.EntityTypeFile,
                FileData = new UpsertFileData
                {
                    Name = LinkRules.NameOrDefault(request.Name, url),
                    Url = url,
                    MimeType = LinkRules.MimeType,
                    StatusCode = "active"
                }
            };
            var result = await _workspaceItemService.UpsertWorkspaceItemsAsync(new List<UpsertWorkspaceItemRequest> { item }, userId, workspaceId);
            if (!result.Success)
                return (0, Fail(result.Status ?? 500, result.Message ?? "Could not create the link"));

            var id = CreatedItemId(result);
            return id.HasValue ? (id.Value, null) : (0, Fail(500, "Link created but its id was not returned"));
        }

        private async Task<ResultOptions?> SoftDeleteItemAsync(WorkspaceItemEntity item, int userId, string? userEmail)
        {
            var request = new UpsertWorkspaceItemRequest
            {
                Action = WorkspaceItemAction.Delete,
                Id = item.Id,
                WorkspaceId = item.WorkspaceId,
                UserId = userId,
                CreatedBy = userEmail
            };
            var result = await _workspaceItemService.UpsertWorkspaceItemsAsync(new List<UpsertWorkspaceItemRequest> { request }, userId, item.WorkspaceId);
            _workspaceService.InvalidateTreeCache(item.WorkspaceId, userId);
            return result.Success ? null : result;
        }

        /// <summary>Batch upsert returns anonymous objects — read the new workspace item id.</summary>
        private static int? CreatedItemId(ResultOptions result)
        {
            var first = result.Data?.FirstOrDefault();
            return first?.GetType().GetProperty("id")?.GetValue(first) as int?;
        }

        private static ResultOptions Ok(List<LinkDto> links, string message) => new ResultOptions
        {
            Success = true,
            Message = message,
            Status = 200,
            Data = links.Cast<object>().ToList(),
            TotalCount = links.Count
        };

        private static ResultOptions Fail(int status, string message) =>
            new ResultOptions { Success = false, Message = message, Status = status };
    }
}
