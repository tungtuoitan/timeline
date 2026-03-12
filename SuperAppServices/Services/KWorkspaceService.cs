using AutoMapper;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    public class KWorkspaceService : IKWorkspaceService
    {
        private readonly IKWorkspaceRepository _workspaceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<KWorkspaceService> _logger;

        public KWorkspaceService(
            IKWorkspaceRepository workspaceRepository,
            IMapper mapper,
            ILogger<KWorkspaceService> logger)
        {
            _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<WsResponse>> GetAllUserWorkspacesAsync(int userId, FilterOptions? filterOptions = null)
        {
            try
            {
                var workspaces = await _workspaceRepository.GetAllWorkspacesByUserIdAsync(userId, filterOptions);
                return _mapper.Map<List<WsResponse>>(workspaces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspaces for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets workspace tree V2 — flat list of self-contained nodes from kws.workspace_items.
        /// </summary>
        public async Task<KWorkspaceDTO> GetWorkspaceTreeV2Async(int workspaceId, int userId, WorkspaceFilterOptions? filterOptions = null)
        {
            try
            {
                _logger.LogInformation("Getting workspace tree V2 for WorkspaceId: {WorkspaceId}", workspaceId);

                var workspaceWithTree = await _workspaceRepository.GetWorkspaceTreeAsync(workspaceId, userId, null);
                if (workspaceWithTree == null)
                    throw new KeyNotFoundException($"Workspace with ID {workspaceId} not found");

                var itemsV2 = workspaceWithTree.Items.Select(item => new KWorkspaceItemResponseV2
                {
                    Id = item.Id,
                    WorkspaceId = item.WorkspaceId,
                    ParentId = item.ParentId,
                    Name = item.Name,
                    Description = item.Description,
                    Color = item.Color,
                    Icon = item.Icon,
                    PathIds = item.PathIds,
                    PathDepth = item.PathDepth,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    DeletedAt = item.DeletedAt
                }).ToList();

                var response = new KWorkspaceDTO
                {
                    Id = workspaceWithTree.WorkspaceId,
                    UserId = workspaceWithTree.UserId,
                    Name = workspaceWithTree.Name,
                    Description = workspaceWithTree.Description,
                    CreatedAt = workspaceWithTree.CreatedAt,
                    UpdatedAt = workspaceWithTree.UpdatedAt,
                    FlatData = itemsV2
                };

                _logger.LogInformation("Retrieved workspace tree V2 with {Count} items for workspace {WorkspaceId}",
                    itemsV2.Count, workspaceId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace tree V2 for WorkspaceId: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        public async Task<ResultOptions> MoveItemsAsync(int workspaceId, int userId, KMoveItemsRequest request)
        {
            try
            {
                _logger.LogInformation("Moving {Count} items in workspace {WorkspaceId}", request.ItemIds.Count, workspaceId);
                return await _workspaceRepository.MoveItemsAsync(workspaceId, request.ItemIds, request.TargetParentId, request.TargetWorkspaceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving items in workspace {WorkspaceId}", workspaceId);
                return new ResultOptions { Success = false, Message = "An error occurred while moving items", Status = 500 };
            }
        }

        public async Task<ResultOptions> DeleteItemsAsync(int workspaceId, int userId, KDeleteItemsRequest request)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} items in workspace {WorkspaceId}", request.ItemIds.Count, workspaceId);
                return await _workspaceRepository.DeleteItemsAsync(workspaceId, request.ItemIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting items in workspace {WorkspaceId}", workspaceId);
                return new ResultOptions { Success = false, Message = "An error occurred while deleting items", Status = 500 };
            }
        }
    }
}
