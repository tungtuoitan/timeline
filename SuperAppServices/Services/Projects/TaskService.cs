using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;
using SuperAppServices.Services.Keywords;

namespace SuperAppServices.Services.Projects
{
    /// <summary>
    /// Service for task operations
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ILogger<TaskService> _logger;
        private readonly KeywordServiceV2 _keywordService;
        private readonly ApplicationDbContext _context;
        private readonly OwnershipGuard _ownership;

        public TaskService(
            ITaskRepository taskRepository,
            IWorkspaceRepository workspaceRepository,
            ILogger<TaskService> logger,
            KeywordServiceV2 keywordService,
            ApplicationDbContext context,
            OwnershipGuard ownership)
        {
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keywordService = keywordService ?? throw new ArgumentNullException(nameof(keywordService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _ownership = ownership ?? throw new ArgumentNullException(nameof(ownership));
        }

        /// <summary>
        /// Get all tasks with optional filters
        /// Returns raw list (no tree building - frontend handles tree)
        /// </summary>
        public async Task<ResultOptions> GetTasksAsync(TaskFilterOptions filterOptions)
        {
            try
            {
                _logger.LogInformation("Getting tasks with filters");

                var result = await _taskRepository.GetTasksAsync(filterOptions);

                if (!result.Success)
                {
                    return result;
                }

                _logger.LogInformation("Successfully retrieved tasks");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tasks");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        public async Task<ResultOptions> GetTaskByIdAsync(int id, int userId)
        {
            return await GetTasksAsync(new TaskFilterOptions { UserId = userId, Ids = new List<int> { id } });
        }

        /// <summary>
        /// Batch upsert multiple tasks (create or update)
        /// </summary>
        public async Task<ResultOptions> UpsertTasksAsync(List<UpsertTaskRequest> requests, int userId)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    _logger.LogWarning("Empty task batch upsert request");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No tasks provided for batch upsert",
                        Status = 400
                    };
                }

                _logger.LogInformation("Batch upserting {Count} tasks", requests.Count);

                // Ownership: target projects, existing tasks, parents and linked folders must all
                // belong to the user — checked before any side effect (folder rename below).
                if (!await _ownership.ProjectsOwnedAsync(userId, requests.Select(r => r.ProjectId)))
                    return OwnershipGuard.Denied("Project");
                if (!await _ownership.TasksOwnedAsync(userId, requests.Where(r => r.Id > 0).Select(r => r.Id)))
                    return OwnershipGuard.Denied("Task");
                if (!await _ownership.TasksOwnedAsync(userId, requests.Where(r => r.ParentTaskId.HasValue).Select(r => r.ParentTaskId!.Value)))
                    return OwnershipGuard.Denied("Parent task");
                if (!await _ownership.WorkspaceItemsOwnedAsync(userId, requests.Where(r => r.FolderWorkspaceItemId.HasValue).Select(r => r.FolderWorkspaceItemId!.Value)))
                    return OwnershipGuard.Denied("Folder");

                // Map requests to entities
                var tasks = new List<ProTask>();

                foreach (var request in requests)
                {
                    // Validation: can't soft delete a new task
                    if (request.DeletedAt.HasValue && request.Id == 0)
                    {
                        _logger.LogError("Cannot set deletedAt on a new task");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "Cannot set deletedAt on a new task. Use existing ID for soft delete/restore.",
                            Status = 400
                        };
                    }

                    // Sync folder name when updating an existing task that has a linked folder
                    if (request.Id > 0 && request.FolderWorkspaceItemId.HasValue)
                    {
                        await _workspaceRepository.UpdateFolderNameByWorkspaceItemIdAsync(request.FolderWorkspaceItemId.Value, request.Title);
                        _logger.LogInformation("Synced folder name to '{Title}' for workspace item ID: {FolderWorkspaceItemId}", request.Title, request.FolderWorkspaceItemId.Value);
                    }

                    var task = new ProTask
                    {
                        Id = request.Id,
                        ProjectId = request.ProjectId,
                        ParentTaskId = request.ParentTaskId,
                        Type = request.Type,
                        TaskType = request.TaskType,
                        Title = request.Title,
                        Description = request.EffectiveDescription, // TODO(0109): remove legacy "note" alias after old FE is gone (use request.Description)
                        Status = request.Status,
                        Priority = request.Priority,
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        OrderIndex = request.OrderIndex,
                        DeletedAt = request.DeletedAt,
                        FolderWorkspaceItemId = request.FolderWorkspaceItemId,
                        ChecklistJson = request.ChecklistJson,
                        ProcessJson = request.ProcessJson,
                        CustomTabsJson = request.CustomTabsJson,
                        IsMilestone = request.IsMilestone,
                    };

                    _logger.LogInformation("TaskService.UpsertTasks - Request Id: {Id}, FolderWorkspaceItemId: {FWI}, HasValue: {HasValue}, Title: '{Title}'",
                        request.Id, request.FolderWorkspaceItemId, request.FolderWorkspaceItemId.HasValue, request.Title);

                    tasks.Add(task);
                }

                var result = await _taskRepository.UpsertTasksAsync(tasks);

                if (result.Success)
                {
                    foreach (var task in tasks.Where(t => t.Id > 0))
                    {
                        try { await _keywordService.SyncTaskKeywordAsync(task.Id, userId); }
                        catch (Exception ex) { _logger.LogError(ex, "Error syncing keyword for task {Id}", task.Id); }
                    }
                }

                if (!result.Success)
                {
                    _logger.LogError("Batch upsert failed: {Message}", result.Message);
                    return result;
                }

                _logger.LogInformation("Batch upsert completed successfully: {Count} tasks upserted",
                    tasks.Count);

                // Log final FolderWorkspaceItemId values after upsert
                if (result.Data != null)
                {
                    foreach (dynamic dto in result.Data)
                    {
                        _logger.LogInformation("TaskService.UpsertTasks - Result Id: {Id}, FolderWorkspaceItemId: {FWI}, Title: '{Title}'",
                            (object)dto.Id, (object)dto.FolderWorkspaceItemId, (object)dto.Title);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during batch upsert");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }
        /// <summary>
        /// Partial update a single task — delegates to repository for merge logic.
        /// </summary>
        public async Task<ResultOptions> PatchTaskAsync(int taskId, PatchTaskRequest request, int userId)
        {
            try
            {
                _logger.LogInformation("Patching task ID: {TaskId} for user: {UserId}", taskId, userId);

                if (!await _ownership.TasksOwnedAsync(userId, new[] { taskId }))
                    return OwnershipGuard.Denied("Task");
                if (request.ProjectId.HasValue && !await _ownership.ProjectsOwnedAsync(userId, new[] { request.ProjectId.Value }))
                    return OwnershipGuard.Denied("Project");
                if (request.ParentTaskId.HasValue)
                {
                    if (request.ParentTaskId.Value == taskId)
                        return new ResultOptions { Success = false, Message = "A task cannot be its own parent", Status = 400 };
                    if (!await _ownership.TasksOwnedAsync(userId, new[] { request.ParentTaskId.Value }))
                        return OwnershipGuard.Denied("Parent task");
                }

                var unknownClears = (request.ClearFields ?? new List<string>())
                    .Where(f => !PatchTaskRequest.ClearableFields.Contains(f)).ToList();
                if (unknownClears.Any())
                    return new ResultOptions { Success = false, Message = $"Cannot clear field(s): {string.Join(", ", unknownClears)}", Status = 400 };
                if (request.Title != null && string.IsNullOrWhiteSpace(request.Title))
                    return new ResultOptions { Success = false, Message = "Title cannot be empty", Status = 400 };
                if (request.Status != null && string.IsNullOrWhiteSpace(request.Status))
                    return new ResultOptions { Success = false, Message = "Status cannot be empty", Status = 400 };

                var result = await _taskRepository.PatchTaskAsync(taskId, request);

                if (result.Success)
                {
                    try { await _keywordService.SyncTaskKeywordAsync(taskId, userId); }
                    catch (Exception ex) { _logger.LogError(ex, "Error syncing keyword for task {Id}", taskId); }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching task ID: {TaskId}", taskId);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Permanently delete tasks and all associated data in one transaction:
        /// comments, checklist history, flow edges, node positions,
        /// workspace folder + notes (soft-deleted), and keywords (HardDeletedAt).
        /// </summary>
        public async Task<ResultOptions> HardDeleteTasksAsync(List<int> taskIds, int userId)
        {
            try
            {
                if (taskIds == null || !taskIds.Any())
                    return new ResultOptions { Success = false, Message = "No task IDs provided", Status = 400 };

                if (!await _ownership.TasksOwnedAsync(userId, taskIds))
                    return OwnershipGuard.Denied("Task");

                var now = DateTime.UtcNow;

                // 1. Load tasks (need FolderWorkspaceItemId)
                var tasks = await _context.ProTasks
                    .Where(t => taskIds.Contains(t.Id))
                    .ToListAsync();

                if (!tasks.Any())
                    return new ResultOptions { Success = false, Message = "Tasks not found", Status = 404 };

                var folderItemIds = tasks
                    .Where(t => t.FolderWorkspaceItemId.HasValue)
                    .Select(t => t.FolderWorkspaceItemId!.Value)
                    .Distinct().ToList();

                // 2. Collect workspace items: folder + direct children (notes)
                var folderWsItems = folderItemIds.Any()
                    ? await _context.WorkspaceItems.Where(i => folderItemIds.Contains(i.Id)).ToListAsync()
                    : new List<WorkspaceItemEntity>();

                var childWsItems = folderItemIds.Any()
                    ? await _context.WorkspaceItems
                        .Where(i => i.ParentId.HasValue && folderItemIds.Contains(i.ParentId.Value))
                        .ToListAsync()
                    : new List<WorkspaceItemEntity>();

                var allWsItems = folderWsItems.Concat(childWsItems).ToList();
                var allWsItemIds = allWsItems.Select(i => i.Id).ToList();
                var noteEntityIds = allWsItems.Where(i => i.EntityType == 3).Select(i => i.EntityId).Distinct().ToList();
                var folderEntityIds = allWsItems.Where(i => i.EntityType == 2).Select(i => i.EntityId).Distinct().ToList();

                // 3. Hard-delete keywords (task + folder/note workspace items)
                var keywordsToMark = await _context.Keywords
                    .Where(k => k.HardDeletedAt == null &&
                        ((k.Type == "task" && taskIds.Contains(k.TargetItemId ?? 0)) ||
                         (allWsItemIds.Any() && allWsItemIds.Contains(k.TargetItemId ?? 0))))
                    .ToListAsync();

                foreach (var kw in keywordsToMark)
                {
                    kw.HardDeletedAt = now;
                    kw.UpdatedAt = now;
                }

                // 4. Delete task comments (null out self-ref parent before removal)
                var comments = await _context.TaskComments
                    .Where(c => taskIds.Contains(c.TaskId))
                    .ToListAsync();
                foreach (var c in comments) c.ParentCommentId = null;
                _context.TaskComments.RemoveRange(comments);

                // 5. Delete task checklist history
                var checklistHistory = await _context.TaskChecklistHistories
                    .Where(h => taskIds.Contains(h.TaskId))
                    .ToListAsync();
                _context.TaskChecklistHistories.RemoveRange(checklistHistory);

                // 6. Delete flow edges (source or target is one of the tasks)
                var flowEdges = await _context.FlowEdges
                    .Where(e => (e.SourceType == "task" && taskIds.Contains(e.SourceId)) ||
                                (e.TargetType == "task" && taskIds.Contains(e.TargetId)))
                    .ToListAsync();
                _context.FlowEdges.RemoveRange(flowEdges);

                // 7. Delete flow node positions
                var positions = await _context.FlowNodePositions
                    .Where(p => p.NodeType == "task" && taskIds.Contains(p.NodeId))
                    .ToListAsync();
                _context.FlowNodePositions.RemoveRange(positions);

                // 8. Null out task.FolderWorkspaceItemId before deleting workspace items
                //    (fk_task_folder_workspace_item would block Save #1 otherwise)
                foreach (var t in tasks) t.FolderWorkspaceItemId = null;

                // 8b. Hard-delete workspace items — children first, then folders (FK_workspace_items_parent)
                if (childWsItems.Any())
                    _context.WorkspaceItems.RemoveRange(childWsItems);
                if (folderWsItems.Any())
                    _context.WorkspaceItems.RemoveRange(folderWsItems);

                // 9. Hard-delete folder entities
                if (folderEntityIds.Any())
                {
                    var folders = await _context.Folders
                        .Where(f => folderEntityIds.Contains(f.Id))
                        .ToListAsync();
                    _context.Folders.RemoveRange(folders);
                }

                // 10. Hard-delete note entities
                if (noteEntityIds.Any())
                {
                    var notes = await _context.Notes
                        .Where(n => noteEntityIds.Contains(n.Id))
                        .ToListAsync();
                    _context.Notes.RemoveRange(notes);
                }


                // 11. Detach child tasks that reference any of the tasks being deleted
                var childTasks = await _context.ProTasks
                    .Where(t => t.ParentTaskId.HasValue && taskIds.Contains(t.ParentTaskId.Value))
                    .ToListAsync();
                foreach (var child in childTasks)
                    child.ParentTaskId = null;

                // Save child records first (comments, edges, positions, ws items, nulled parents),
                // then delete the tasks — two saves to satisfy all FK constraints in correct order.
                await _context.SaveChangesAsync();

                _context.ProTasks.RemoveRange(tasks);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Hard-deleted {Count} tasks and related data (comments: {C}, edges: {E}, positions: {P}, wsItems: {W}) for userId: {UserId}",
                    tasks.Count, comments.Count, flowEdges.Count, positions.Count, allWsItems.Count, userId);

                return new ResultOptions { Success = true, Message = $"Permanently deleted {tasks.Count} task(s)" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard-deleting tasks [{TaskIds}]", string.Join(",", taskIds));
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}