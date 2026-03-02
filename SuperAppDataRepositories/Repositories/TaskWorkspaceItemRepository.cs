using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for TaskWorkspaceItem data access (pro.TaskWorkspaceItem table)
    /// </summary>
    public class TaskWorkspaceItemRepository : ITaskWorkspaceItemRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskWorkspaceItemRepository> _logger;

        public TaskWorkspaceItemRepository(
            ApplicationDbContext context,
            ILogger<TaskWorkspaceItemRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all workspace items linked to a task
        /// </summary>
        public async Task<ResultOptions> GetByTaskIdAsync(int taskId)
        {
            try
            {
                _logger.LogInformation("Getting workspace items for taskId: {TaskId}", taskId);

                var items = await _context.TaskWorkspaceItems
                    .AsNoTracking()
                    .Where(t => t.TaskId == taskId)
                    .ToListAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = $"Retrieved {items.Count} workspace items for task {taskId}",
                    Data = items.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace items for taskId: {TaskId}", taskId);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Creates a new TaskWorkspaceItem link
        /// </summary>
        public async Task<ResultOptions> CreateAsync(TaskWorkspaceItem item)
        {
            try
            {
                _logger.LogInformation("Creating TaskWorkspaceItem - TaskId: {TaskId}, WorkspaceItemId: {WorkspaceItemId}, ItemType: {ItemType}",
                    item.TaskId, item.WorkspaceItemId, item.ItemType);

                // Check for duplicate
                var exists = await _context.TaskWorkspaceItems
                    .AnyAsync(t => t.TaskId == item.TaskId && t.WorkspaceItemId == item.WorkspaceItemId);

                if (exists)
                {
                    _logger.LogWarning("TaskWorkspaceItem already exists - TaskId: {TaskId}, WorkspaceItemId: {WorkspaceItemId}",
                        item.TaskId, item.WorkspaceItemId);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "This workspace item is already linked to the task",
                        Status = 409
                    };
                }

                _context.TaskWorkspaceItems.Add(item);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created TaskWorkspaceItem with ID: {Id}", item.Id);

                return new ResultOptions
                {
                    Success = true,
                    Message = "Workspace item linked to task successfully",
                    Object = item,
                    Status = 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating TaskWorkspaceItem");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Deletes a TaskWorkspaceItem link
        /// </summary>
        public async Task<ResultOptions> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting TaskWorkspaceItem with ID: {Id}", id);

                var item = await _context.TaskWorkspaceItems.FindAsync(id);

                if (item == null)
                {
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"TaskWorkspaceItem with ID {id} not found",
                        Status = 404
                    };
                }

                _context.TaskWorkspaceItems.Remove(item);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted TaskWorkspaceItem with ID: {Id}", id);

                return new ResultOptions
                {
                    Success = true,
                    Message = "Workspace item unlinked from task successfully",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting TaskWorkspaceItem with ID: {Id}", id);
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
