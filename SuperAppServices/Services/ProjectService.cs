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
        private readonly IWsRepository _wsRepository;
        private readonly ILogger<ProjectService> _logger;
        private readonly KeywordServiceV2 _keywordService;

        public ProjectService(
            IProjectRepository projectRepository,
            IWsRepository wsRepository,
            ILogger<ProjectService> logger,
            KeywordServiceV2 keywordService)
        {
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _wsRepository = wsRepository ?? throw new ArgumentNullException(nameof(wsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keywordService = keywordService ?? throw new ArgumentNullException(nameof(keywordService));
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

        public async Task<ResultOptions> GetProjectByIdAsync(int id, int userId)
        {
            return await GetProjectsAsync(new ProjectFilterOptions { UserId = userId, Ids = new List<int> { id } });
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

                    // Auto-create workspace for new projects only
                    if (request.Id == 0 && !request.WorkspaceId.HasValue)
                    {
                        _logger.LogInformation("Auto-creating workspace for project: '{Name}'", request.Name);
                        var workspace = new Workspace
                        {
                            Name = request.Name,
                            UserId = request.UserId,
                            StatusCode = "active"
                        };
                        var wsResult = await _wsRepository.UpsertWorkspaceAsync(workspace);
                        if (wsResult.Success && wsResult.Object is Workspace createdWs)
                        {
                            request.WorkspaceId = createdWs.Id;
                            _logger.LogInformation("Auto-created workspace ID: {WorkspaceId} for project: '{Name}'", createdWs.Id, request.Name);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to auto-create workspace for project: '{Name}'. Continuing without workspace.", request.Name);
                        }
                    }
                    // Sync workspace name when updating an existing project that has a workspace
                    else if (request.Id > 0 && request.WorkspaceId.HasValue)
                    {
                        await _wsRepository.UpdateWorkspaceNameAsync(request.WorkspaceId.Value, request.Name);
                        _logger.LogInformation("Synced workspace name to '{Name}' for workspace ID: {WorkspaceId}", request.Name, request.WorkspaceId.Value);
                    }

                    var project = new Project
                    {
                        Id = request.Id,
                        UserId = request.UserId,
                        Name = request.Name,
                        Description = request.Description,
                        Status = request.Status,
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        DeletedAt = request.DeletedAt,
                        WorkspaceId = request.WorkspaceId,
                        Image = request.Image
                    };

                    projects.Add(project);
                }

                var result = await _projectRepository.UpsertProjectsAsync(projects);

                if (result.Success)
                {
                    foreach (var project in projects.Where(p => p.Id > 0))
                    {
                        try { await _keywordService.SyncProjectKeywordAsync(project.Id, project.UserId); }
                        catch (Exception ex) { _logger.LogError(ex, "Error syncing keyword for project {Id}", project.Id); }
                    }
                }

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
