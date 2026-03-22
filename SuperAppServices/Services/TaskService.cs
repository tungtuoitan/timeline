using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
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

        public TaskService(
            ITaskRepository taskRepository,
            IWorkspaceRepository workspaceRepository,
            ILogger<TaskService> logger,
            KeywordServiceV2 keywordService)
        {
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keywordService = keywordService ?? throw new ArgumentNullException(nameof(keywordService));
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
                        Note = request.Note,
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
                    };

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
    }
}