using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Helper for k.node action-based operations.
    /// k.node is a self-contained node table — no external entity tables.
    /// </summary>
    public class KNodeHelperService : IKNodeHelperService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KNodeHelperService> _logger;

        public KNodeHelperService(
            ApplicationDbContext context,
            ILogger<KNodeHelperService> logger)
        {
            _context = context;
            _logger  = logger;
        }

        // =====================================================================
        // VALIDATION
        // =====================================================================

        public string? ValidateRequest(KUpsertNodeRequest request, Dictionary<int, KNodeEntity> existingNodes)
        {
            switch (request.Action)
            {
                case KNodeAction.Create:
                    if (request.NodeData == null)
                        return "Create action requires NodeData";
                    if (string.IsNullOrWhiteSpace(request.NodeData.Name))
                        return "Create action requires a non-empty name";
                    if (request.ParentId.HasValue && request.ParentId.Value <= 0)
                        return "Create action: parentId must be a positive integer or null";
                    return null;

                case KNodeAction.Update:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Update action requires valid Id";
                    if (!existingNodes.ContainsKey(request.Id.Value))
                        return $"Node ID {request.Id} not found";
                    if (request.NodeData == null)
                        return "Update action requires NodeData";
                    return null;

                case KNodeAction.Move:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Move action requires valid Id";
                    if (!existingNodes.ContainsKey(request.Id.Value))
                        return $"Node ID {request.Id} not found";
                    if (request.ParentId.HasValue && request.ParentId.Value <= 0)
                        return "Move action: parentId must be a positive integer or null";
                    return null;

                case KNodeAction.MoveCross:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "MoveCross action requires valid Id";
                    if (!request.KnowledgeId.HasValue || request.KnowledgeId.Value <= 0)
                        return "MoveCross action requires valid target KnowledgeId";
                    if (!existingNodes.ContainsKey(request.Id.Value))
                        return $"Node ID {request.Id} not found";
                    if (existingNodes[request.Id.Value].KnowledgeId == request.KnowledgeId.Value)
                        return "MoveCross requires target knowledge to differ from source knowledge";
                    if (request.ParentId.HasValue && request.ParentId.Value <= 0)
                        return "MoveCross action: parentId must be a positive integer or null";
                    return null;

                case KNodeAction.Delete:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Delete action requires valid Id";
                    if (!existingNodes.ContainsKey(request.Id.Value))
                        return $"Node ID {request.Id} not found";
                    return null;

                case KNodeAction.Restore:
                    if (!request.Id.HasValue || request.Id.Value <= 0)
                        return "Restore action requires valid Id";
                    if (!existingNodes.ContainsKey(request.Id.Value))
                        return $"Node ID {request.Id} not found";
                    if (existingNodes[request.Id.Value].DeletedAt == null)
                        return $"Node ID {request.Id} is not deleted, cannot restore";
                    return null;

                default:
                    return $"Unsupported action: {request.Action}";
            }
        }

        public async Task<string?> CheckCircularDependencyAsync(
            int nodeId,
            int newParentId,
            Dictionary<int, KNodeEntity> existingNodes)
        {
            if (nodeId == newParentId)
                return $"Cannot move node {nodeId} to itself";

            var currentParentId = newParentId;
            var visited = new HashSet<int> { nodeId };
            const int maxDepth = 100;
            var depth = 0;

            while (currentParentId > 0 && depth < maxDepth)
            {
                if (visited.Contains(currentParentId))
                    return $"Circular dependency detected: moving node {nodeId} to parent {newParentId} would create a cycle";

                visited.Add(currentParentId);

                KNodeEntity? parent = existingNodes.TryGetValue(currentParentId, out var cached)
                    ? cached
                    : await _context.KNodes.FindAsync(currentParentId);

                if (parent == null || !parent.ParentId.HasValue)
                    break;

                currentParentId = parent.ParentId.Value;
                depth++;
            }

            if (depth >= maxDepth)
                return $"Maximum tree depth exceeded when checking circular dependency for node {nodeId}";

            return null;
        }

        // =====================================================================
        // ACTION PROCESSING
        // =====================================================================

        public async Task ProcessCreateAsync(
            KUpsertNodeRequest request,
            int userId,
            int knowledgeId,
            List<KNodeEntity> upserted)
        {
            var data = request.NodeData!;
            var newNode = new KNodeEntity
            {
                KnowledgeId = request.KnowledgeId ?? knowledgeId,
                ParentId    = request.ParentId,
                Name        = data.Name,
                Description = data.Description,
                Color       = data.Color ?? "#F59E0B",
                Icon        = data.Icon  ?? "📁",
                CreatedAt   = DateTime.UtcNow,
                DeletedAt   = null
            };

            _context.KNodes.Add(newNode);
            await _context.SaveChangesAsync();

            upserted.Add(newNode);
            _logger.LogInformation("Created node ID {Id} (Name={Name})", newNode.Id, newNode.Name);
        }

        public void ProcessUpdate(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            var node = existingNodes[request.Id!.Value];
            var data = request.NodeData!;

            node.Name        = data.Name;
            node.Description = data.Description;
            if (data.Color != null) node.Color = data.Color;
            if (data.Icon  != null) node.Icon  = data.Icon;
            node.UpdatedAt = DateTime.UtcNow;

            upserted.Add(node);
            _logger.LogInformation("Updated node ID {Id} (Name={Name})", node.Id, node.Name);
        }

        public void ProcessMove(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            var node = existingNodes[request.Id!.Value];
            node.ParentId  = request.ParentId;
            node.UpdatedAt = DateTime.UtcNow;

            upserted.Add(node);
            _logger.LogInformation("Moved node ID {Id} → ParentId={ParentId}", node.Id, node.ParentId);
        }

        public async Task ProcessMoveCrossAsync(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            var root = existingNodes[request.Id!.Value];
            var targetKnowledgeId = request.KnowledgeId!.Value;

            root.KnowledgeId = targetKnowledgeId;
            root.ParentId    = request.ParentId > 0 ? request.ParentId : null;
            root.UpdatedAt   = DateTime.UtcNow;
            upserted.Add(root);

            var descendants = await GetAllDescendantsAsync(root.Id);
            foreach (var d in descendants)
            {
                d.KnowledgeId = targetKnowledgeId;
                d.UpdatedAt   = DateTime.UtcNow;
                upserted.Add(d);
            }

            _logger.LogInformation("MoveCross: node {Id} + {Count} descendants → knowledge {KnowledgeId}",
                root.Id, descendants.Count, targetKnowledgeId);
        }

        public void ProcessDelete(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            var node = existingNodes[request.Id!.Value];
            node.DeletedAt = DateTime.UtcNow;
            node.UpdatedAt = DateTime.UtcNow;
            upserted.Add(node);
            _logger.LogInformation("Soft-deleted node ID {Id}", node.Id);
        }

        public void ProcessRestore(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            var node = existingNodes[request.Id!.Value];
            node.DeletedAt = null;
            node.UpdatedAt = DateTime.UtcNow;
            upserted.Add(node);
            _logger.LogInformation("Restored node ID {Id}", node.Id);
        }

        public async Task<List<KNodeEntity>> GetAllDescendantsAsync(int parentNodeId)
        {
            var descendants = new List<KNodeEntity>();
            var queue = new Queue<int>();
            queue.Enqueue(parentNodeId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = await _context.KNodes
                    .Where(n => n.ParentId == currentId)
                    .ToListAsync();

                foreach (var child in children)
                {
                    descendants.Add(child);
                    queue.Enqueue(child.Id);
                }
            }

            return descendants;
        }

        // =====================================================================
        // PATH SYNC
        // =====================================================================

        public async Task SyncPathIdsAsync(List<KNodeEntity> upserted)
        {
            foreach (var node in upserted.Where(n => !n.DeletedAt.HasValue))
            {
                try { await RebuildPathIdsAsync(node); }
                catch (Exception ex) { _logger.LogError(ex, "Failed to rebuild PathIds for node {Id}", node.Id); }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RebuildPathIdsAsync(KNodeEntity node)
        {
            string newPathIds;
            int newDepth;

            if (node.ParentId.HasValue)
            {
                var parent = await _context.Set<KNodeEntity>().FindAsync(node.ParentId.Value);
                if (parent == null)
                {
                    _logger.LogWarning("Parent {ParentId} not found for node {NodeId}", node.ParentId.Value, node.Id);
                    return;
                }
                newPathIds = $"{parent.PathIds}{node.Id}/";
                newDepth   = parent.PathDepth + 1;
            }
            else
            {
                newPathIds = $"/{node.Id}/";
                newDepth   = 1;
            }

            if (node.PathIds != newPathIds || node.PathDepth != newDepth)
            {
                var oldPathIds = node.PathIds;
                node.PathIds   = newPathIds;
                node.PathDepth = newDepth;

                _logger.LogInformation("Updated PathIds for node {Id}: {Old} → {New}", node.Id, oldPathIds, newPathIds);

                if (!string.IsNullOrEmpty(oldPathIds) && oldPathIds != "/" && oldPathIds != newPathIds)
                    await UpdateDescendantPathIdsAsync(node.Id, oldPathIds, newPathIds);
            }
        }

        public async Task UpdateDescendantPathIdsAsync(int parentId, string oldPathIds, string newPathIds)
        {
            var descendants = await _context.Set<KNodeEntity>()
                .Where(n => n.PathIds.StartsWith(oldPathIds) && n.Id != parentId)
                .ToListAsync();

            foreach (var d in descendants)
            {
                d.PathIds   = d.PathIds.Replace(oldPathIds, newPathIds);
                d.PathDepth = d.PathIds.Count(c => c == '/') - 1;
            }

            _logger.LogInformation("Updated PathIds for {Count} descendants of node {ParentId}", descendants.Count, parentId);
        }
    }
}
