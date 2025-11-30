using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Common.Exceptions;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Workspaces.Queries.GetWorkspaceTree
{
    public class GetWorkspaceTreeQueryHandler : IRequestHandler<GetWorkspaceTreeQuery, WorkspaceWithTreeResponse>
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetWorkspaceTreeQueryHandler> _logger;

        public GetWorkspaceTreeQueryHandler(
            IWorkspaceRepository workspaceRepository,
            IMapper mapper,
            ILogger<GetWorkspaceTreeQueryHandler> logger)
        {
            _workspaceRepository = workspaceRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<WorkspaceWithTreeResponse> Handle(GetWorkspaceTreeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting workspace tree for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    request.WorkspaceId, request.UserId);

                // Get workspace info
                var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(request.WorkspaceId, request.UserId);
                if (workspace == null)
                {
                    _logger.LogWarning("Workspace {WorkspaceId} not found for user {UserId}",
                        request.WorkspaceId, request.UserId);
                    throw new NotFoundException($"Workspace with ID {request.WorkspaceId} not found");
                }

                // Get workspace tree (tags, notes, files) using NEW method
                var treeItems = await _workspaceRepository.GetWorkspaceTreeAsync(request.WorkspaceId, request.UserId);

                // Map to WorkspaceTreeItemResponse using AutoMapper
                var flatResponse = _mapper.Map<List<WorkspaceTreeItemResponse>>(treeItems);

                // Build hierarchical structure
                var hierarchicalItems = BuildHierarchy(flatResponse);

                // Create response
                var response = new WorkspaceWithTreeResponse
                {
                    WorkspaceId = workspace.WorkspaceId,
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
                    IsArchived = false,
                    TagCount = 0,
                    MemberCount = 1,
                    Settings = null,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    Items = hierarchicalItems // Polymorphic tree items (tags, notes, files)
                };

                _logger.LogInformation("Successfully retrieved workspace tree with {ItemCount} root items for workspace {WorkspaceId}",
                    hierarchicalItems.Count, request.WorkspaceId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspace tree for WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    request.WorkspaceId, request.UserId);
                throw;
            }
        }

        /// <summary>
        /// Build hierarchical tree structure from flat list
        /// Uses dictionary-based parent lookup for O(1) performance
        /// Uses composite key (ItemType + ItemId) to handle cases where different item types have the same ID
        /// </summary>
        private List<WorkspaceTreeItemResponse> BuildHierarchy(List<WorkspaceTreeItemResponse> flatItems)
        {
            // Log duplicate detection for debugging
            var duplicates = flatItems
                .GroupBy(i => new { i.ItemType, i.ItemId })
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Any())
            {
                foreach (var dup in duplicates)
                {
                    _logger.LogWarning(
                        "Found {Count} duplicate items with ItemType={ItemType}, ItemId={ItemId}",
                        dup.Count(), dup.Key.ItemType, dup.Key.ItemId);
                }
            }

            // Dictionary for O(1) parent lookup - use composite key (ItemType + ItemId)
            // This handles cases where Tag.TagId=127 and Note.NoteId=127 both exist
            var itemDict = flatItems.ToDictionary(
                i => $"{i.ItemType}_{i.ItemId}", 
                i => i);
            
            var rootItems = new List<WorkspaceTreeItemResponse>();

            foreach (var item in flatItems)
            {
                if (item.ParentId == null)
                {
                    // Root item (no parent)
                    rootItems.Add(item);
                }
                else
                {
                    // Child item - parent is ALWAYS a tag (only tags can be parents)
                    var parentKey = $"tag_{item.ParentId.Value}";
                    
                    if (itemDict.TryGetValue(parentKey, out var parent))
                    {
                        // Add to parent's children
                        parent.Children.Add(item);
                    }
                    else
                    {
                        // Orphan (parent tag not found) - treat as root
                        _logger.LogWarning(
                            "Item {ItemType}_{ItemId} has ParentId={ParentId} (tag) which was not found. Treating as root.",
                            item.ItemType, item.ItemId, item.ParentId);
                        rootItems.Add(item);
                    }
                }
            }

            // Sort children recursively
            SortItemsRecursively(rootItems);

            return rootItems;
        }

        /// <summary>
        /// Sort items alphabetically by name (case-insensitive), recursively
        /// </summary>
        private void SortItemsRecursively(List<WorkspaceTreeItemResponse> items)
        {
            items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            foreach (var item in items)
            {
                if (item.Children.Any())
                {
                    SortItemsRecursively(item.Children);
                }
            }
        }
    }
}
