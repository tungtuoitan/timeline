using AutoMapper;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for workspace list operations (ws.workspaces)
    /// </summary>
    public class WorkspaceListService : IWorkspaceListService
    {
        private readonly IWorkspaceListRepository _workspaceListRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<WorkspaceListService> _logger;

        public WorkspaceListService(
            IWorkspaceListRepository workspaceListRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<WorkspaceListService> logger)
        {
            _workspaceListRepository = workspaceListRepository ?? throw new ArgumentNullException(nameof(workspaceListRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all workspaces with optional filters
        /// </summary>
        public async Task<ResultOptions> GetWorkspacesAsync(bool getAll, string? searchText)
        {
            try
            {
                _logger.LogInformation("Getting workspaces with GetAll: {GetAll}, SearchText: {SearchText}",
                    getAll, searchText);

                var filterOptions = new WorkspaceFilterOptions
                {
                    GetAll = getAll,
                    SearchText = searchText
                };

                // Repository returns ResultOptions
                var result = await _workspaceListRepository.GetWorkspacesAsync(filterOptions);
                
                if (!result.Success)
                {
                    return result; // Return repository error as-is
                }

                // Map Workspace entities to WorkspaceDTO
                var workspaces = result.Data?.Cast<Workspace>().ToList() ?? new List<Workspace>();
                var response = _mapper.Map<List<WorkspaceDTO>>(workspaces);

                _logger.LogInformation("Successfully retrieved {Count} workspaces", response.Count);
                
                return new ResultOptions
                {
                    Success = true,
                    Message = "Workspaces retrieved successfully",
                    Data = response.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspaces");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Get workspace by ID
        /// </summary>
        public async Task<ResultOptions> GetWorkspaceByIdAsync(int workspaceId)
        {
            try
            {
                _logger.LogInformation("Getting workspace with ID: {WorkspaceId}", workspaceId);

                // Repository returns ResultOptions
                var result = await _workspaceListRepository.GetWorkspaceById(workspaceId);

                if (!result.Success)
                {
                    return result; // Return repository error as-is
                }

                // Map Workspace entity to WorkspaceDTO
                var workspace = result.Object as Workspace;
                if (workspace != null)
                {
                    var response = _mapper.Map<WorkspaceDTO>(workspace);
                    _logger.LogInformation("Successfully retrieved workspace with ID: {WorkspaceId}", workspaceId);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Workspace retrieved successfully",
                        Object = response,
                        Status = 200
                    };
                }

                _logger.LogWarning("Workspace not found with ID: {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Workspace not found with ID: {workspaceId}",
                    Status = 404
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspace with ID: {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Create or update workspace (upsert)
        /// </summary>
        public async Task<ResultOptions> UpsertWorkspaceAsync(UpsertWorkspaceRequest request)
        {
            try
            {
                _logger.LogInformation("Processing workspace with ID: {WorkspaceId}, Name: '{Name}'",
                    request.Id,
                    request.Name);

                var workspace = _mapper.Map<Workspace>(request);
                workspace.Id = request.Id ?? 0;

                // Set user ID from request
                if (request.UserId.HasValue)
                {
                    workspace.UserId = request.UserId.Value;
                }

                // Upsert workspace (create or update)
                var result = await _workspaceListRepository.UpsertWorkspaceAsync(workspace);

                if (!result.Success)
                {
                    return result; // Return repository error as-is
                }

                // Map result to DTO
                var savedWorkspace = result.Object as Workspace;
                if (savedWorkspace != null)
                {
                    var response = _mapper.Map<WorkspaceDTO>(savedWorkspace);
                    _logger.LogInformation("Successfully upserted workspace with ID: {WorkspaceId}", savedWorkspace.Id);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = workspace.Id > 0 ? "Workspace updated successfully" : "Workspace created successfully",
                        Object = response,
                        Status = workspace.Id > 0 ? 200 : 201
                    };
                }

                return new ResultOptions
                {
                    Success = false,
                    Message = "Failed to upsert workspace",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while upserting workspace");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Delete workspaces by IDs with cascade to all items (folders/notes/files)
        /// </summary>
        public async Task<ResultOptions> DeleteWorkspacesAsync(string workspaceIds, bool isHardDelete = false)
        {
            try
            {
                _logger.LogInformation("Deleting workspaces with IDs: {WorkspaceIds} (HardDelete: {IsHardDelete})",
                    workspaceIds, isHardDelete);

                var result = await _workspaceListRepository.DeleteWorkspacesCascadeAsync(workspaceIds, isHardDelete);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully deleted workspaces with cascade");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting workspaces");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Restore deleted workspaces by setting deleted_at to null
        /// </summary>
        public async Task<ResultOptions> UndoDeleteWorkspacesAsync(List<int> workspaceIds)
        {
            try
            {
                _logger.LogInformation("Restoring workspaces with IDs: {WorkspaceIds}",
                    string.Join(",", workspaceIds));

                var result = await _workspaceListRepository.UndoDeleteWorkspacesBatchAsync(workspaceIds);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully restored workspaces");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while restoring workspaces");
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
