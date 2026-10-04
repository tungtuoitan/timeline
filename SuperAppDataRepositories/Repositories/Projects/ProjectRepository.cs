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
    /// Repository for project data access
    /// </summary>
    public class ProjectRepository : IProjectRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectRepository> _logger;

        public ProjectRepository(
            ApplicationDbContext context,
            ILogger<ProjectRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all projects with filtering options
        /// </summary>
        public async Task<ResultOptions> GetProjectsAsync(ProjectFilterOptions filterOptions)
        {
            try
            {
                _logger.LogInformation("Getting projects for userId: {UserId}", filterOptions.UserId);

                var query = _context.Projects.AsNoTracking();

                // Filter by user ID (required for data isolation)
                query = query.Where(p => p.UserId == filterOptions.UserId);

                // Filter by IDs if provided
                if (filterOptions.Ids?.Count > 0)
                {
                    query = query.Where(p => filterOptions.Ids.Contains(p.Id));
                }

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                {
                    query = query.Where(p => p.Name.Contains(filterOptions.SearchText) ||
                                           (p.Description != null && p.Description.Contains(filterOptions.SearchText)));
                }

                // Filter by status codes (multi-select)
                if (filterOptions.StatusCodes?.Count > 0)
                {
                    query = query.Where(p => filterOptions.StatusCodes.Contains(p.Status));
                }

                // Filter by deletedAt
                if (!string.IsNullOrEmpty(filterOptions.DeletedAt))
                {
                    if (filterOptions.DeletedAt == "null")
                    {
                        query = query.Where(p => p.DeletedAt == null);
                    }
                    else if (filterOptions.DeletedAt == "notNull")
                    {
                        query = query.Where(p => p.DeletedAt != null);
                    }
                }

                // Order by created_at descending
                query = query.OrderByDescending(p => p.CreatedAt);

                var projects = await query.ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} projects", projects.Count);

                return new ResultOptions
                {
                    Success = true,
                    Message = "Projects retrieved successfully",
                    Data = projects.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting projects");
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error while retrieving projects",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Batch upserts multiple projects in a single transaction (all-or-nothing)
        /// </summary>
        public async Task<ResultOptions> UpsertProjectsAsync(List<Project> projects)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (projects == null || !projects.Any())
                    {
                        _logger.LogWarning("Empty project batch upsert request");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "No projects provided for batch upsert",
                            Status = 400
                        };
                    }

                    _logger.LogInformation("Starting batch upsert transaction for {Count} projects", projects.Count);

                    // Get all project IDs that need to be updated (Id > 0)
                    var projectIdsToUpdate = projects
                        .Where(p => p.Id > 0)
                        .Select(p => p.Id)
                        .ToList();

                    // Load all existing projects in one query
                    var existingProjectsDict = await _context.Projects
                        .Where(p => projectIdsToUpdate.Contains(p.Id))
                        .ToDictionaryAsync(p => p.Id, p => p);

                    var upsertedProjects = new List<Project>();

                    foreach (var project in projects)
                    {
                        bool isUpdate = project.Id > 0 && existingProjectsDict.ContainsKey(project.Id);

                        if (isUpdate)
                        {
                            // UPDATE existing project
                            var existingProject = existingProjectsDict[project.Id];

                            _logger.LogInformation("Updating project ID: {ProjectId}, Name: '{Name}'",
                                project.Id, project.Name);

                            existingProject.Name = project.Name;
                            existingProject.Description = project.Description;
                            existingProject.Status = project.Status;
                            existingProject.StartDate = project.StartDate;
                            existingProject.EndDate = project.EndDate;
                            existingProject.DeletedAt = project.DeletedAt;
                            existingProject.WorkspaceId = project.WorkspaceId;
                            existingProject.Image = project.Image;
                            existingProject.UpdatedAt = DateTime.UtcNow;
                            // UserId is immutable after creation

                            upsertedProjects.Add(existingProject);
                        }
                        else
                        {
                            // CREATE new project
                            _logger.LogInformation("Creating project with Name: '{Name}'", project.Name);

                            project.CreatedAt = DateTime.UtcNow;
                            project.UpdatedAt = DateTime.UtcNow;
                            project.DeletedAt = null;
                            // UserId should already be set from the request

                            _context.Projects.Add(project);
                            upsertedProjects.Add(project);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully committed batch upsert transaction for {Count} projects",
                        projects.Count);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully upserted {projects.Count} projects",
                        Data = upsertedProjects.Cast<object>().ToList(),
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
    }
}
