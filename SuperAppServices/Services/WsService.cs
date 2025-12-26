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
    public class WsService : IWsService
    {
        private readonly IWsRepository _wsRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<WsService> _logger;

        public WsService( 
            IWsRepository wsRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<WsService> logger)
        {
            _wsRepository = wsRepository ?? throw new ArgumentNullException(nameof(wsRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all workspaces with optional filters
        /// </summary>
        public async Task<ResultOptions> GetWorkspacesAsync(int userId, string? searchText)
        {
            try
            {
                _logger.LogInformation("Getting workspaces for UserId: {UserId}, SearchText: {SearchText}",
                    userId, searchText);

                var filterOptions = new WorkspaceFilterOptions
                {
                    UserId = userId,  // ✅ Filter by userId
                    SearchText = searchText
                };

                // Repository returns ResultOptions
                var result = await _wsRepository.GetWorkspacesAsync(filterOptions);
                
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
                var result = await _wsRepository.GetWorkspaceById(workspaceId);

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
        /// Batch upsert multiple workspaces (create or update)
        /// For single workspace operations, pass a list with 1 element
        /// </summary>
        public async Task<ResultOptions> UpsertWorkspacesBatchAsync(List<UpsertWorkspaceRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    _logger.LogWarning("Empty workspace batch upsert request");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No workspaces provided for batch upsert",
                        Status = 400
                    };
                }

                _logger.LogInformation("Batch upserting {Count} workspaces", requests.Count);

                var successCount = 0;
                var failCount = 0;
                var errors = new List<string>();
                var results = new List<WorkspaceDTO>();

                foreach (var request in requests)
                {
                    try
                    {
                        _logger.LogInformation("Processing workspace with ID: {WorkspaceId}, Name: '{Name}'",
                            request.Id,
                            request.Name);

                        var workspace = _mapper.Map<Workspace>(request);
                        workspace.Id = request.Id ?? 0;
                        workspace.DeletedAt = request.DeletedAt;  // Map deletedAt for soft delete/restore

                        // Validation: can't soft delete a new workspace
                        if (request.DeletedAt.HasValue && workspace.Id == 0)
                        {
                            throw new ArgumentException("Cannot set deletedAt on a new workspace. Use ID > 0 for soft delete/restore.");
                        }

                        // Set user ID from request
                        if (request.UserId.HasValue)
                        {
                            workspace.UserId = request.UserId.Value;
                        }

                        // Upsert workspace (create or update)
                        var result = await _wsRepository.UpsertWorkspaceAsync(workspace);

                        if (!result.Success)
                        {
                            errors.Add($"Workspace ID {request.Id}: {result.Message}");
                            failCount++;
                            continue;
                        }

                        // Map result to DTO
                        var savedWorkspace = result.Object as Workspace;
                        if (savedWorkspace != null)
                        {
                            var response = _mapper.Map<WorkspaceDTO>(savedWorkspace);
                            results.Add(response);
                            successCount++;
                            _logger.LogInformation("Successfully upserted workspace with ID: {WorkspaceId}", savedWorkspace.Id);
                        }
                        else
                        {
                            errors.Add($"Workspace ID {request.Id}: Failed to process workspace");
                            failCount++;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "Invalid argument while processing workspace with ID: {WorkspaceId}", request.Id);
                        errors.Add($"Workspace ID {request.Id}: {ex.Message}");
                        failCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while upserting workspace with ID: {WorkspaceId}", request.Id);
                        errors.Add($"Workspace ID {request.Id}: {ex.Message}");
                        failCount++;
                    }
                }

                var message = successCount > 0
                    ? $"Successfully upserted {successCount}/{requests.Count} workspaces"
                    : "Failed to upsert all workspaces";

                if (failCount > 0)
                {
                    message += $". {failCount} failed.";
                }

                _logger.LogInformation("Batch upsert completed: {SuccessCount} succeeded, {FailCount} failed",
                    successCount, failCount);

                return new ResultOptions
                {
                    Success = successCount > 0,
                    Message = message,
                    Object = new
                    {
                        SuccessCount = successCount,
                        FailCount = failCount,
                        Errors = errors,
                        Workspaces = results
                    },
                    Status = successCount > 0 ? 200 : 400
                };
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
        /// Delete workspaces by IDs with cascade to all items (folders/notes/files)
        /// </summary>
        public async Task<ResultOptions> DeleteWorkspacesAsync(string workspaceIds)
        {
            try
            {
                _logger.LogInformation("Deleting workspaces with IDs: {WorkspaceIds})",
                    workspaceIds);

                var result = await _wsRepository.DeleteWorkspacesCascadeAsync(workspaceIds);

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

      
    }
}
