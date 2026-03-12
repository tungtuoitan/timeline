using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SuperAppModels.Models;
using SuperAppModels.DTOs.Requests;
using SuperAppDataRepositories.Data;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Helper for K workspace item action-based operations.
    /// kws.workspace_items is a self-contained node table (no separate entity tables).
    /// </summary>
    public class KWorkspaceItemHelperService : IKWorkspaceItemHelperService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KWorkspaceItemHelperService> _logger;

        public KWorkspaceItemHelperService(
            ApplicationDbContext context,
            ILogger<KWorkspaceItemHelperService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =====================================================================
        // VALIDATION
        // =====================================================================

        public string? ValidateRequest(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict)
        {
            switch (request.Action)
            {
                case KWorkspaceItemAction.Create:
                    if (request.NodeData == null)
                        return "Create action requires NodeData";
                    if (string.IsNullOrWhiteSpace(request.NodeData.Name))
                        return "Create action requires a non-empty name";
                    return null;

                case KWorkspaceItemAction.Update:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Update action requires valid Id";
                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";
                    if (request.NodeData == null)
                        return "Update action requires NodeData";
                    return null;

                case KWorkspaceItemAction.Move:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Move action requires valid Id";
                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";
                    return null;

                case KWorkspaceItemAction.MoveCross:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "MoveCross action requires valid Id";
                    if (!request.WorkspaceId.HasValue || request.WorkspaceId.Value <= 0)
                        return "MoveCross action requires valid target WorkspaceId";
                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";
                    var sourceItem = existingItemsDict[request.Id.Value];
                    if (sourceItem.WorkspaceId == request.WorkspaceId.Value)
                        return "MoveCross requires target workspace to be different from source workspace";
                    return null;

                case KWorkspaceItemAction.Delete:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Delete action requires valid Id";
                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";
                    return null;

                case KWorkspaceItemAction.Restore:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Restore action requires valid Id";
                    if (!existingItemsDict.ContainsKey(request.Id.Value))
                        return $"Workspace item ID {request.Id} not found";
                    var itemToRestore = existingItemsDict[request.Id.Value];
                    if (itemToRestore.DeletedAt == null)
                        return $"Workspace item ID {request.Id} is not deleted, cannot restore";
                    return null;

                default:
                    return $"Unsupported action: {request.Action}";
            }
        }

        public async Task<string?> CheckCircularDependencyAsync(
            int itemId,
            int newParentId,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict)
        {
            if (itemId == newParentId)
                return $"Cannot move item {itemId} to itself";

            var currentParentId = newParentId;
            var visited = new HashSet<int> { itemId };
            var maxDepth = 100;
            var depth = 0;

            while (currentParentId > 0 && depth < maxDepth)
            {
                if (visited.Contains(currentParentId))
                    return $"Circular dependency detected: moving item {itemId} to parent {newParentId} would create a cycle";

                visited.Add(currentParentId);

                KWorkspaceItemEntity? parent = existingItemsDict.TryGetValue(currentParentId, out var cached)
                    ? cached
                    : await _context.KWorkspaceItems.FindAsync(currentParentId);

                if (parent == null || !parent.ParentId.HasValue)
                    break;

                currentParentId = parent.ParentId.Value;
                depth++;
            }

            if (depth >= maxDepth)
                return $"Maximum tree depth exceeded when checking circular dependency for item {itemId}";

            return null;
        }

        // =====================================================================
        // ACTION PROCESSING
        // =====================================================================

        public async Task ProcessCreateActionAsync(
            KUpsertWorkspaceItemRequest request,
            int userId,
            int workspaceId,
            List<KWorkspaceItemEntity> upsertedItems)
        {
            var data = request.NodeData!;
            var newItem = new KWorkspaceItemEntity
            {
                WorkspaceId = request.WorkspaceId ?? workspaceId,
                ParentId = request.ParentId,
                Name = data.Name,
                Description = data.Description,
                Color = data.Color ?? "#F59E0B",
                Icon = data.Icon ?? "📁",
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null
            };

            _context.KWorkspaceItems.Add(newItem);
            await _context.SaveChangesAsync();

            upsertedItems.Add(newItem);

            _logger.LogInformation("Created workspace_item ID {Id} (Name={Name})", newItem.Id, newItem.Name);
        }

        public void ProcessUpdateNodeAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems)
        {
            var item = existingItemsDict[request.Id!.Value];
            var data = request.NodeData!;

            item.Name = data.Name;
            item.Description = data.Description;
            if (data.Color != null) item.Color = data.Color;
            if (data.Icon != null) item.Icon = data.Icon;
            item.UpdatedAt = DateTime.UtcNow;

            upsertedItems.Add(item);

            _logger.LogInformation("Updated workspace_item ID {Id} (Name={Name})", item.Id, item.Name);
        }

        public void ProcessMoveAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems)
        {
            var item = existingItemsDict[request.Id!.Value];

            item.ParentId = request.ParentId;
            if (request.WorkspaceId.HasValue)
                item.WorkspaceId = request.WorkspaceId.Value;
            item.UpdatedAt = DateTime.UtcNow;

            upsertedItems.Add(item);

            _logger.LogInformation("Moved workspace_item ID {Id} to ParentId={ParentId}", item.Id, item.ParentId);
        }

        public async Task ProcessMoveCrossActionAsync(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems)
        {
            var rootItem = existingItemsDict[request.Id!.Value];
            var targetWorkspaceId = request.WorkspaceId!.Value;

            rootItem.WorkspaceId = targetWorkspaceId;
            rootItem.ParentId = request.ParentId;
            rootItem.UpdatedAt = DateTime.UtcNow;
            upsertedItems.Add(rootItem);

            var descendants = await GetAllDescendantsAsync(rootItem.Id);
            foreach (var descendant in descendants)
            {
                descendant.WorkspaceId = targetWorkspaceId;
                descendant.UpdatedAt = DateTime.UtcNow;
                upsertedItems.Add(descendant);
            }

            _logger.LogInformation("MoveCross: moved item {Id} + {Count} descendants to workspace {WsId}",
                rootItem.Id, descendants.Count, targetWorkspaceId);
        }

        public async Task<List<KWorkspaceItemEntity>> GetAllDescendantsAsync(int parentWorkspaceItemId)
        {
            var descendants = new List<KWorkspaceItemEntity>();
            var queue = new Queue<int>();
            queue.Enqueue(parentWorkspaceItemId);

            while (queue.Count > 0)
            {
                var currentParentId = queue.Dequeue();
                var children = await _context.KWorkspaceItems
                    .Where(wi => wi.ParentId == currentParentId)
                    .ToListAsync();

                foreach (var child in children)
                {
                    descendants.Add(child);
                    queue.Enqueue(child.Id);
                }
            }

            return descendants;
        }

        public void ProcessDeleteAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems)
        {
            var item = existingItemsDict[request.Id!.Value];
            item.DeletedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            upsertedItems.Add(item);

            _logger.LogInformation("Soft deleted workspace_item ID {Id}", item.Id);
        }

        public void ProcessRestoreAction(
            KUpsertWorkspaceItemRequest request,
            Dictionary<int, KWorkspaceItemEntity> existingItemsDict,
            List<KWorkspaceItemEntity> upsertedItems)
        {
            var item = existingItemsDict[request.Id!.Value];
            item.DeletedAt = null;
            item.UpdatedAt = DateTime.UtcNow;
            upsertedItems.Add(item);

            _logger.LogInformation("Restored workspace_item ID {Id}", item.Id);
        }

        // =====================================================================
        // PATH SYNC
        // =====================================================================

        public async Task SyncPathIdsAsync(List<KWorkspaceItemEntity> upsertedItems)
        {
            foreach (var item in upsertedItems)
            {
                if (!item.DeletedAt.HasValue)
                {
                    try
                    {
                        await RebuildPathIdsAsync(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to rebuild PathIds for workspace item {Id}", item.Id);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RebuildPathIdsAsync(KWorkspaceItemEntity item)
        {
            string newPathIds;
            int newDepth;

            if (item.ParentId.HasValue)
            {
                var parent = await _context.Set<KWorkspaceItemEntity>().FindAsync(item.ParentId.Value);
                if (parent == null)
                {
                    _logger.LogWarning("Parent {ParentId} not found for item {ItemId}", item.ParentId.Value, item.Id);
                    return;
                }

                newPathIds = $"{parent.PathIds}{item.Id}/";
                newDepth = parent.PathDepth + 1;
            }
            else
            {
                newPathIds = $"/{item.Id}/";
                newDepth = 1;
            }

            if (item.PathIds != newPathIds || item.PathDepth != newDepth)
            {
                var oldPathIds = item.PathIds;
                item.PathIds = newPathIds;
                item.PathDepth = newDepth;

                _logger.LogInformation("Updated PathIds for item {Id}: {Old} → {New}", item.Id, oldPathIds, newPathIds);

                if (!string.IsNullOrEmpty(oldPathIds) && oldPathIds != "/" && oldPathIds != newPathIds)
                {
                    await UpdateDescendantPathIdsAsync(item.Id, oldPathIds, newPathIds);
                }
            }
        }

        public async Task UpdateDescendantPathIdsAsync(int parentId, string oldPathIds, string newPathIds)
        {
            var descendants = await _context.Set<KWorkspaceItemEntity>()
                .Where(i => i.PathIds.StartsWith(oldPathIds) && i.Id != parentId)
                .ToListAsync();

            foreach (var descendant in descendants)
            {
                descendant.PathIds = descendant.PathIds.Replace(oldPathIds, newPathIds);
                descendant.PathDepth = descendant.PathIds.Count(c => c == '/') - 1;
            }

            _logger.LogInformation("Updated PathIds for {Count} descendants of item {ParentId}",
                descendants.Count, parentId);
        }
    }
}
