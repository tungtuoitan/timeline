using AutoMapper;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
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

                // Map to WorkspaceTreeItemResponse using AutoMapper
                var flatResponse = _mapper.Map<List<WorkspaceTreeItemResponse>>(workspaceWithTree.Items);

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
                    hierarchicalItems.Count, workspaceId);

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
