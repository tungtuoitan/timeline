using AutoMapper;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for workspace operations
    /// </summary>
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(
            IWorkspaceRepository workspaceRepository,
            IMapper mapper,
            ILogger<WorkspaceService> logger)
        {
            _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all workspaces for a user
        /// </summary>
        public async Task<List<WorkspaceListResponse>> GetAllUserWorkspacesAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting all workspaces for UserId: {UserId}", userId);

                var workspaces = await _workspaceRepository.GetAllWorkspacesByUserIdAsync(userId);

                var response = _mapper.Map<List<WorkspaceListResponse>>(workspaces);

                _logger.LogInformation("Successfully retrieved {Count} workspaces for user {UserId}",
                    response.Count, userId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspaces for UserId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets workspace tree with all items (tags, notes, files)
        /// </summary>
        public async Task<WorkspaceWithTreeResponse> GetWorkspaceTreeAsync(int workspaceId, int userId)
        {
            try
            {
                _logger.LogInformation("Getting workspace tree for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);

                // Get workspace info
                var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(workspaceId, userId);
                if (workspace == null)
                {
                    _logger.LogWarning("Workspace {WorkspaceId} not found for user {UserId}",
                        workspaceId, userId);
                    throw new KeyNotFoundException($"Workspace with ID {workspaceId} not found");
                }

                // Get workspace tree (tags, notes, files)
                var workspaceWithTree = await _workspaceRepository.GetWorkspaceTreeAsync(workspaceId, userId);
                if (workspaceWithTree == null)
                {
                    _logger.LogWarning("Workspace tree data not found for workspace {WorkspaceId}", workspaceId);
                    throw new InvalidOperationException($"Failed to retrieve workspace tree for workspace {workspaceId}");
                }

                // Map to WorkspaceItemResponse using AutoMapper
                var flatResponse = _mapper.Map<List<WorkspaceItemResponse>>(workspaceWithTree.Items);

                // ✅ Return FLAT data - Frontend will build hierarchy when needed
                // This improves: payload size, caching, real-time updates, flexibility

                // Create response
                var response = new WorkspaceWithTreeResponse
                {
                    WorkspaceId = workspace.Id,
                    UserId = workspace.UserId,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    // Default values for properties not in DB schema
                    Color = "#3B82F6", // Default blue
                    Icon = "📁",
                    Type = "hierarchy",
                    MaxDepth = 10,
                    IsDefault = false,
                    IsPublic = false,
                    IsTemplate = false,
                    TagCount = 0,
                    MemberCount = 1,
                    Settings = null,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    Items = flatResponse // ✅ FLAT list with parentId - Frontend builds hierarchy
                };

                _logger.LogInformation("Successfully retrieved workspace tree with {ItemCount} items for workspace {WorkspaceId}",
                    flatResponse.Count, workspaceId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspace tree for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                throw;
            }
        }

       

        /// <summary>
        /// Creates a new folder in a workspace
        /// </summary>
        public async Task<ResultOptions> UpsertFolderAsync(int workspaceId, int userId, UpsertFolderRequest request)
        {
            try
            {
                var isUpdate = request.Id.HasValue;
                var action = isUpdate ? "Updating" : "Creating";
                
                _logger.LogInformation("{Action} folder '{Name}' in workspace {WorkspaceId} for user {UserId}, ParentId: {ParentId}",
                    action, request.Name, workspaceId, userId, request.ParentId);

                // Call repository to upsert folder
                var result = await _workspaceRepository.UpsertFolderAsync(
                    workspaceId,
                    userId,
                    request
                );

                // Check if repository operation was successful
                if (!result.Success)
                {
                    _logger.LogWarning("Repository failed to {Action} folder: {Message}", action.ToLower(), result.Message);
                    return result;
                }

                // Map folder object to response DTO
                var folderResponse = _mapper.Map<FolderResponse>(result.Object);

                var successMessage = isUpdate ? "Folder updated successfully" : "Folder created successfully";
                _logger.LogInformation("Successfully {Action} folder {FolderId} in workspace {WorkspaceId}",
                    action.ToLower(), result.Reference, workspaceId);

                // Return ResultOptions with mapped response
                return new ResultOptions
                {
                    Success = true,
                    Message = successMessage,
                    Reference = result.Reference,
                    Object = folderResponse,
                    Status = isUpdate ? 200 : 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting folder '{Name}' in workspace {WorkspaceId}",
                    request.Name, workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while upserting folder",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Moves multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        public async Task<ResultOptions> MoveItemsAsync(int workspaceId, int userId, MoveItemsRequest request)
        {
            try
            {
                _logger.LogInformation("Moving {Count} items in workspace {WorkspaceId} for user {UserId}",
                    request.Items.Count, workspaceId, userId);

                // Convert request items to tuple list
                var items = request.Items
                    .Select(i => (i.Type, i.Id))
                    .ToList();

                // Call repository
                var result = await _workspaceRepository.MoveItemsAsync(
                    workspaceId,
                    items,
                    request.TargetParentId,
                    request.TargetWorkspaceId
                );

                if (result.Success)
                {
                    _logger.LogInformation("Successfully moved {Count} items in workspace {WorkspaceId}",
                        request.Items.Count, workspaceId);
                }
                else
                {
                    _logger.LogWarning("Failed to move items: {Message}", result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving items in workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while moving items",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Deletes multiple workspace items (folders/notes/files) with cascade support
        /// </summary>
        public async Task<ResultOptions> DeleteItemsAsync(int workspaceId, int userId, DeleteItemsRequest request)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} items in workspace {WorkspaceId} for user {UserId} (HardDelete: {IsHardDelete})",
                    request.Items.Count, workspaceId, userId, request.IsHardDelete);

                // Convert request items to tuple list
                var items = request.Items
                    .Select(i => (i.Type, i.Id))
                    .ToList();

                // Call repository
                var result = await _workspaceRepository.DeleteItemsAsync(workspaceId, items, request.IsHardDelete);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully deleted {Count} items in workspace {WorkspaceId}",
                        request.Items.Count, workspaceId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete items: {Message}", result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting items in workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while deleting items",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Adds an item (folder/note/file) to a workspace
        /// </summary>
        public async Task<ResultOptions> AddItemToWorkspaceAsync(int workspaceId, int userId, AddItemToWorkspaceRequest request)
        {
            try
            {
                _logger.LogInformation("Adding {ChildType} (ID: {ChildId}) to workspace {WorkspaceId} under parent {ParentId}",
                    request.ChildType, request.ChildId, workspaceId, request.ParentTagId);

                // Call repository to add item
                var result = await _workspaceRepository.AddItemToWorkspaceAsync(workspaceId, userId, request);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully added {ChildType} to workspace {WorkspaceId}",
                        request.ChildType, workspaceId);
                }
                else
                {
                    _logger.LogWarning("Failed to add item: {Message}", result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to workspace {WorkspaceId}", workspaceId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "An error occurred while adding item to workspace",
                    Status = 500
                };
            }
        }
    }
}
