using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for task data access
    /// </summary>
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskRepository> _logger;

        public TaskRepository(
            ApplicationDbContext context,
            ILogger<TaskRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all tasks with filtering options
        /// Returns raw list (no tree building)
        /// Tasks are filtered by user through project ownership
        /// </summary>
        public async Task<ResultOptions> GetTasksAsync(TaskFilterOptions filterOptions)
        {
            try
            {
                _logger.LogInformation("Getting tasks for userId: {UserId}", filterOptions.UserId);

                // Get user's project IDs first for data isolation
                var userProjectIds = await _context.Projects
                    .Where(p => p.UserId == filterOptions.UserId)
                    .Select(p => p.Id)
                    .ToListAsync();

                var query = _context.ProTasks.AsNoTracking();

                // Filter by user's projects (required for data isolation)
                query = query.Where(t => userProjectIds.Contains(t.ProjectId));

                // Filter by specific project IDs (must be subset of user's projects)
                if (filterOptions.ProjectIds?.Count > 0)
                {
                    var validProjectIds = filterOptions.ProjectIds.Intersect(userProjectIds).ToList();
                    query = query.Where(t => validProjectIds.Contains(t.ProjectId));
                }

                // Filter by task IDs
                if (filterOptions.Ids?.Count > 0)
                {
                    query = query.Where(t => filterOptions.Ids.Contains(t.Id));
                }

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                {
                    query = query.Where(t => t.Title.Contains(filterOptions.SearchText) ||
                                           (t.Note != null && t.Note.Contains(filterOptions.SearchText)));
                }

                // Filter by status
                if (!string.IsNullOrWhiteSpace(filterOptions.Status))
                {
                    query = query.Where(t => filterOptions.Status.Contains(t.Status));
                }

                // Filter by priority
                if (!string.IsNullOrWhiteSpace(filterOptions.Priority))
                {
                    query = query.Where(t => filterOptions.Priority.Contains(t.Priority));
                }

                // Filter by type
                if (!string.IsNullOrWhiteSpace(filterOptions.Type))
                {
                    query = query.Where(t => t.Type == filterOptions.Type);
                }

                // Filter by deletedAt
                if (!string.IsNullOrEmpty(filterOptions.DeletedAt))
                {
                    if (filterOptions.DeletedAt == "null")
                    {
                        query = query.Where(t => t.DeletedAt == null);
                    }
                    else if (filterOptions.DeletedAt == "notNull")
                    {
                        query = query.Where(t => t.DeletedAt != null);
                    }
                }

                // Order by order_index, then created_at
                query = query.OrderBy(t => t.OrderIndex).ThenBy(t => t.CreatedAt);

                // Join with projects and parent tasks to get limit dates
                var tasksWithLimits = await query
                    .Join(
                        _context.Projects,
                        task => task.ProjectId,
                        project => project.Id,
                        (task, project) => new { Task = task, Project = project }
                    )
                    .GroupJoin(
                        _context.ProTasks,
                        tp => tp.Task.ParentTaskId,
                        parentTask => parentTask.Id,
                        (tp, parentTasks) => new { tp.Task, tp.Project, ParentTasks = parentTasks }
                    )
                    .SelectMany(
                        x => x.ParentTasks.DefaultIfEmpty(),
                        (x, parentTask) => new ProTask
                        {
                            Id = x.Task.Id,
                            ProjectId = x.Task.ProjectId,
                            ParentTaskId = x.Task.ParentTaskId,
                            Type = x.Task.Type,
                            TaskType = x.Task.TaskType,
                            Title = x.Task.Title,
                            Note = x.Task.Note,
                            Status = x.Task.Status,
                            Priority = x.Task.Priority,
                            StartDate = x.Task.StartDate,
                            EndDate = x.Task.EndDate,
                            OrderIndex = x.Task.OrderIndex,
                            CreatedAt = x.Task.CreatedAt,
                            UpdatedAt = x.Task.UpdatedAt,
                            DeletedAt = x.Task.DeletedAt,
                            FolderWorkspaceItemId = x.Task.FolderWorkspaceItemId,
                            ChecklistJson = x.Task.ChecklistJson,
                            ProcessJson = x.Task.ProcessJson,
                            CustomTabsJson = x.Task.CustomTabsJson,
                            // Limit dates from project
                            ProjectStartDate = x.Project.StartDate,
                            ProjectEndDate = x.Project.EndDate,
                            // Limit dates from parent task (null if no parent)
                            ParentStartDate = parentTask != null ? parentTask.StartDate : null,
                            ParentEndDate = parentTask != null ? parentTask.EndDate : null
                        }
                    )
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} tasks with limit dates", tasksWithLimits.Count);

                return new ResultOptions
                {
                    Success = true,
                    Message = "Tasks retrieved successfully",
                    Data = tasksWithLimits.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting tasks");
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error while retrieving tasks",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Batch upserts multiple tasks in a single transaction (all-or-nothing)
        /// </summary>
        public async Task<ResultOptions> UpsertTasksAsync(List<ProTask> tasks)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (tasks == null || !tasks.Any())
                    {
                        _logger.LogWarning("Empty task batch upsert request");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "No tasks provided for batch upsert",
                            Status = 400
                        };
                    }

                    _logger.LogInformation("Starting batch upsert transaction for {Count} tasks", tasks.Count);

                    // Validate all project IDs exist
                    var projectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();
                    var existingProjectIds = await _context.Projects
                        .Where(p => projectIds.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToListAsync();

                    var missingProjectIds = projectIds.Except(existingProjectIds).ToList();
                    if (missingProjectIds.Any())
                    {
                        await transaction.RollbackAsync();
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"Projects not found with IDs: {string.Join(", ", missingProjectIds)}",
                            Status = 400
                        };
                    }

                    // Get all task IDs that need to be updated (Id > 0)
                    var taskIdsToUpdate = tasks
                        .Where(t => t.Id > 0)
                        .Select(t => t.Id)
                        .ToList();

                    // Load all existing tasks in one query
                    var existingTasksDict = await _context.ProTasks
                        .Where(t => taskIdsToUpdate.Contains(t.Id))
                        .ToDictionaryAsync(t => t.Id, t => t);

                    var upsertedTasks = new List<ProTask>();

                    foreach (var task in tasks)
                    {
                        bool isUpdate = task.Id > 0 && existingTasksDict.ContainsKey(task.Id);

                        if (isUpdate)
                        {
                            // UPDATE existing task
                            var existingTask = existingTasksDict[task.Id];

                            _logger.LogInformation("Updating task ID: {TaskId}, Title: '{Title}'",
                                task.Id, task.Title);

                            existingTask.ProjectId = task.ProjectId;
                            existingTask.ParentTaskId = task.ParentTaskId;
                            existingTask.Type = task.Type;
                            existingTask.TaskType = task.TaskType;
                            existingTask.ChecklistJson = task.ChecklistJson;
                            existingTask.ProcessJson = task.ProcessJson;
                            existingTask.CustomTabsJson = task.CustomTabsJson;
                            existingTask.Title = task.Title;
                            existingTask.Note = task.Note;
                            existingTask.Status = task.Status;
                            existingTask.Priority = task.Priority;
                            existingTask.StartDate = task.StartDate;
                            existingTask.EndDate = task.EndDate;
                            existingTask.OrderIndex = task.OrderIndex;
                            existingTask.DeletedAt = task.DeletedAt;
                            existingTask.FolderWorkspaceItemId = task.FolderWorkspaceItemId;
                            existingTask.UpdatedAt = DateTime.UtcNow;

                            upsertedTasks.Add(existingTask);
                        }
                        else
                        {
                            // CREATE new task
                            _logger.LogInformation("Creating task with Title: '{Title}'", task.Title);

                            task.CreatedAt = DateTime.UtcNow;
                            task.UpdatedAt = DateTime.UtcNow;

                            _context.ProTasks.Add(task);
                            upsertedTasks.Add(task);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully committed batch upsert transaction for {Count} tasks",
                        tasks.Count);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully upserted {tasks.Count} tasks",
                        Data = upsertedTasks.Cast<object>().ToList(),
                        Status = 200
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency conflict during batch upsert - transaction rolled back");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Concurrency conflict occurred - all changes rolled back",
                        Status = 409
                    };
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database error during batch upsert - transaction rolled back");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Database error occurred - all changes rolled back",
                        Status = 500
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during batch upsert - transaction rolled back");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = ex.Message + " - all changes rolled back",
                        Status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Partial update: load existing task, merge non-null fields, save, return updated task with project/parent dates.
        /// </summary>
        public async Task<ResultOptions> PatchTaskAsync(int taskId, PatchTaskRequest request)
        {
            try
            {
                var existing = await _context.ProTasks.FindAsync(taskId);
                if (existing == null)
                {
                    return new ResultOptions { Success = false, Message = $"Task with ID {taskId} not found", Status = 404 };
                }

                // Merge only non-null fields
                if (request.Note != null) existing.Note = request.Note;
                if (request.ChecklistJson != null) existing.ChecklistJson = request.ChecklistJson;
                if (request.ProcessJson != null) existing.ProcessJson = request.ProcessJson;
                if (request.CustomTabsJson != null) existing.CustomTabsJson = request.CustomTabsJson;
                if (request.Status != null) existing.Status = request.Status;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Patched task ID: {TaskId}", taskId);

                // Re-query to get project/parent limit dates (same pattern as GetTasksAsync)
                var project = await _context.Projects.FindAsync(existing.ProjectId);
                ProTask? parentTask = existing.ParentTaskId.HasValue
                    ? await _context.ProTasks.FindAsync(existing.ParentTaskId.Value)
                    : null;

                existing.ProjectStartDate = project?.StartDate;
                existing.ProjectEndDate = project?.EndDate;
                existing.ParentStartDate = parentTask?.StartDate;
                existing.ParentEndDate = parentTask?.EndDate;

                return new ResultOptions
                {
                    Success = true,
                    Message = "Task patched successfully",
                    Data = new List<object> { existing },
                    Status = 200
                };
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
