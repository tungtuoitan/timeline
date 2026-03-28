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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ValidateRequest for action {Action}", request.Action);
                throw;
            }
        }

        public async Task<string?> CheckCircularDependencyAsync(
            int nodeId,
            int newParentId,
            Dictionary<int, KNodeEntity> existingNodes)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking circular dependency for node {NodeId} → parent {ParentId}", nodeId, newParentId);
                throw;
            }
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
            try
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
                    NodeType    = data.NodeType,
                    CreatedAt   = DateTime.UtcNow,
                    DeletedAt   = null
                };

                _context.KNodes.Add(newNode);
                await _context.SaveChangesAsync();

                upserted.Add(newNode);
                _logger.LogInformation("Created node ID {Id} (Name={Name})", newNode.Id, newNode.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Create for node name={Name}", request.NodeData?.Name);
                throw;
            }
        }

        public void ProcessUpdate(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            try
            {
                var node = existingNodes[request.Id!.Value];
                var data = request.NodeData!;

                node.Name        = data.Name;
                node.Description = data.Description;
                if (data.Color != null) node.Color = data.Color;
                if (data.Icon  != null) node.Icon  = data.Icon;
                if (data.NodeType  != null) node.NodeType = data.NodeType;
                if (data.StatusCode  != null) node.StatusCode = data.StatusCode;
                node.UpdatedAt = DateTime.UtcNow;

                upserted.Add(node);
                _logger.LogInformation("Updated node ID {Id} (Name={Name})", node.Id, node.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Update for node ID {NodeId}", request.Id);
                throw;
            }
        }

        public void ProcessMove(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            try
            {
                var node = existingNodes[request.Id!.Value];
                node.ParentId  = request.ParentId;
                node.UpdatedAt = DateTime.UtcNow;

                upserted.Add(node);
                _logger.LogInformation("Moved node ID {Id} → ParentId={ParentId}", node.Id, node.ParentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Move for node ID {NodeId}", request.Id);
                throw;
            }
        }

        public async Task ProcessMoveCrossAsync(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoveCross for node ID {NodeId}", request.Id);
                throw;
            }
        }

        public void ProcessDelete(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            try
            {
                var node = existingNodes[request.Id!.Value];
                node.DeletedAt = DateTime.UtcNow;
                node.UpdatedAt = DateTime.UtcNow;
                upserted.Add(node);
                _logger.LogInformation("Soft-deleted node ID {Id}", node.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Delete for node ID {NodeId}", request.Id);
                throw;
            }
        }

        public void ProcessRestore(
            KUpsertNodeRequest request,
            Dictionary<int, KNodeEntity> existingNodes,
            List<KNodeEntity> upserted)
        {
            try
            {
                var node = existingNodes[request.Id!.Value];
                node.DeletedAt = null;
                node.UpdatedAt = DateTime.UtcNow;
                upserted.Add(node);
                _logger.LogInformation("Restored node ID {Id}", node.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Restore for node ID {NodeId}", request.Id);
                throw;
            }
        }

        public async Task<List<KNodeEntity>> GetAllDescendantsAsync(int parentNodeId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all descendants for node {ParentNodeId}", parentNodeId);
                throw;
            }
        }

        // =====================================================================
        // PATH SYNC
        // =====================================================================

        public async Task SyncPathIdsAsync(List<KNodeEntity> upserted)
        {
            try
            {
                foreach (var node in upserted.Where(n => !n.DeletedAt.HasValue))
                {
                    try { await RebuildPathIdsAsync(node); }
                    catch (Exception ex) { _logger.LogError(ex, "Failed to rebuild PathIds for node {Id}", node.Id); }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SyncPathIds for {Count} nodes", upserted.Count);
                throw;
            }
        }

        public async Task RebuildPathIdsAsync(KNodeEntity node)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding PathIds for node {NodeId}", node.Id);
                throw;
            }
        }

        public async Task UpdateDescendantPathIdsAsync(int parentId, string oldPathIds, string newPathIds)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating descendant PathIds for parent {ParentId}", parentId);
                throw;
            }
        }
    }
}
