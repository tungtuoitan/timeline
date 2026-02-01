using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for project operations
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(
            IProjectRepository projectRepository,
            ILogger<ProjectService> logger)
        {
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all projects with optional filters
        /// </summary>
        public async Task<ResultOptions> GetProjectsAsync(ProjectFilterOptions filterOptions)
        {
            try
            {
                _logger.LogInformation("Getting projects with filters");

                var result = await _projectRepository.GetProjectsAsync(filterOptions);

                if (!result.Success)
                {
                    return result;
                }

                _logger.LogInformation("Successfully retrieved projects");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting projects");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Batch upsert multiple projects (create or update)
        /// </summary>
        public async Task<ResultOptions> UpsertProjectsAsync(List<UpsertProjectRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    _logger.LogWarning("Empty project batch upsert request");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No projects provided for batch upsert",
                        Status = 400
                    };
                }

                _logger.LogInformation("Batch upserting {Count} projects", requests.Count);

                // Map requests to entities
                var projects = new List<Project>();

                foreach (var request in requests)
                {
                    // Validation: can't soft delete a new project
                    if (request.DeletedAt.HasValue && request.Id == 0)
                    {
                        _logger.LogError("Cannot set deletedAt on a new project");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "Cannot set deletedAt on a new project. Use existing ID for soft delete/restore.",
                            Status = 400
                        };
                    }

                    var project = new Project
                    {
                        Id = request.Id,
                        Name = request.Name,
                        Description = request.Description,
                        Status = request.Status,
                        DeletedAt = request.DeletedAt
                    };

                    projects.Add(project);
                }

                var result = await _projectRepository.UpsertProjectsAsync(projects);

                if (!result.Success)
                {
                    _logger.LogError("Batch upsert failed: {Message}", result.Message);
                    return result;
                }

                _logger.LogInformation("Batch upsert completed successfully: {Count} projects upserted",
                    projects.Count);

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
    }
}
