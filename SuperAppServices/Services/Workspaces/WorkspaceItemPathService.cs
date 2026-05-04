using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppModels.Models;

namespace SuperAppServices.Services.Workspaces
{
    /// <summary>
    /// Service for managing WorkspaceItem PathIds (Materialized Path operations)
    /// </summary>
    public class WorkspaceItemPathService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkspaceItemPathService> _logger;

        public WorkspaceItemPathService(
            ApplicationDbContext context,
            ILogger<WorkspaceItemPathService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Create Operations

        /// <summary>
        /// Create workspace item with PathIds
        /// </summary>
        public async Task<WorkspaceItemEntity> CreateItemAsync(
            int workspaceId,
            int? parentId,
            byte entityType,
            int entityId,
            string name)
        {
            var item = new WorkspaceItemEntity
            {
                WorkspaceId = workspaceId,
                ParentId = parentId,
                EntityType = entityType,
                EntityId = entityId,
                //Slug = GenerateSlug(name)
            };

            // Build PathIds
            // NOTE: PathIds does NOT contain workspaceId (workspace is not in workspace_items)
            // Workspace is considered depth=0 (root), folders start at depth=1
            if (parentId.HasValue)
            {
                var parent = await _context.Set<WorkspaceItemEntity>().FindAsync(parentId.Value);
                if (parent == null)
                    throw new ArgumentException($"Parent item {parentId} not found");

                // Child item: append to parent's path
                // Example: parent=/175/ (depth=1) → child=/175/{newId}/ (depth=2)
                item.PathIds = $"{parent.PathIds}{item.Id}/";
                item.PathDepth = parent.PathDepth + 1;

                if (item.PathDepth > 11)
                    throw new InvalidOperationException("Maximum nesting depth (11) exceeded");
            }
            else
            {
                // Root folder in workspace (ParentId=NULL): depth = 1
                // Workspace is considered depth=0 (not in workspace_items)
                // Example: /{itemId}/ where itemId is a root folder (first level child of workspace)
                item.PathIds = "/";
                item.PathDepth = 1;
            }

            _context.Set<WorkspaceItemEntity>().Add(item);
            await _context.SaveChangesAsync();

            // Update PathIds with actual ID (after insert - ID is auto-generated)
            // Before SaveChanges: item.Id = 0, PathIds = "/"
            // After SaveChanges:  item.Id = 175 (generated)
            // Now update:         PathIds = "/175/"
            if (!parentId.HasValue)
            {
                // Root folder: PathIds = /{itemId}/, PathDepth = 1
                // Workspace is considered depth=0 (not in workspace_items)
                // Example: /175/ (root folder - first level child of workspace)
                item.PathIds = $"/{item.Id}/";
            }
            else
            {
                // Child item: PathIds = {parent.PathIds}{itemId}/, PathDepth = parent.PathDepth + 1
                // Example: parent=/175/ (depth=1) → child=/175/174/ (depth=2)
                var parent = await _context.Set<WorkspaceItemEntity>().FindAsync(parentId.Value);
                item.PathIds = $"{parent!.PathIds}{item.Id}/";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created workspace item {Id} with PathIds {PathIds}", item.Id, item.PathIds);

            return item;
        }

        #endregion

        #region Update Operations

        /// <summary>
        /// Rename item (update name/slug, does NOT affect PathIds)
        /// </summary>
        public async Task RenameItemAsync(int itemId, string newName)
        {
            var item = await _context.Set<WorkspaceItemEntity>().FindAsync(itemId);
            if (item == null)
                throw new ArgumentException($"Item {itemId} not found");

            //item.Slug = GenerateSlug(newName);
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Renamed item {Id} to {Name}", itemId, newName);

            // Note: No need to update children - PathIds unchanged!
        }

        #endregion

        #region Move Operations

        /// <summary>
        /// Move item to new parent (updates PathIds for item + all descendants)
        /// </summary>
        public async Task MoveItemAsync(int itemId, int? newParentId)
        {
            var item = await _context.Set<WorkspaceItemEntity>().FindAsync(itemId);
            if (item == null)
                throw new ArgumentException($"Item {itemId} not found");

            var oldPathIds = item.PathIds;
            string newPathIds;
            int newDepth;

            if (newParentId.HasValue)
            {
                var newParent = await _context.Set<WorkspaceItemEntity>().FindAsync(newParentId.Value);
                if (newParent == null)
                    throw new ArgumentException($"Parent item {newParentId} not found");

                // Check for circular reference
                if (newParent.PathIds.Contains($"/{itemId}/"))
                    throw new InvalidOperationException("Cannot move item into its own descendant");

                newPathIds = $"{newParent.PathIds}{itemId}/";
                newDepth = newParent.PathDepth + 1;

                if (newDepth > 11)
                    throw new InvalidOperationException("Maximum nesting depth (11) exceeded");
            }
            else
            {
                // Move to root (first level child of workspace)
                // Workspace is considered depth=0 (not in workspace_items)
                newPathIds = $"/{itemId}/";
                newDepth = 1;
            }

            // Update item
            item.ParentId = newParentId;
            item.PathIds = newPathIds;
            item.PathDepth = newDepth;
            item.UpdatedAt = DateTime.UtcNow;

            // Update all descendants (CASCADE UPDATE)
            var descendants = await _context.Set<WorkspaceItemEntity>()
                .Where(i => i.PathIds.StartsWith(oldPathIds) && i.Id != itemId)
                .ToListAsync();

            _logger.LogInformation("Moving item {Id}: updating {Count} descendants", itemId, descendants.Count);

            foreach (var descendant in descendants)
            {
                // Replace old path prefix with new path prefix
                descendant.PathIds = descendant.PathIds.Replace(oldPathIds, newPathIds);

                // Calculate PathDepth from PathIds
                // NOTE: PathIds does NOT contain workspaceId (workspace is not in workspace_items)
                // Workspace is considered depth=0 (root), folders start at depth=1
                // Formula: PathDepth = Count('/') - 1
                // Examples:
                //   /175/          → 2 slashes → 2 - 1 = 1 (root folder - first level child of workspace)
                //   /175/174/      → 3 slashes → 3 - 1 = 2 (depth 2 - nested folder)
                //   /175/174/180/  → 4 slashes → 4 - 1 = 3 (depth 3 - nested folder)
                //   /a/b/c/d/e/    → 6 slashes → 6 - 1 = 5 (depth 5 - deeply nested)
                descendant.PathDepth = descendant.PathIds.Count(c => c == '/') - 1;

                descendant.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Moved item {Id} from {OldPath} to {NewPath}", itemId, oldPathIds, newPathIds);

            // Return both old and new paths for keyword service
            await OnItemMovedAsync(oldPathIds, newPathIds);
        }

        /// <summary>
        /// Hook for keyword service to rebuild links after move
        /// </summary>
        private async Task OnItemMovedAsync(string oldPathIds, string newPathIds)
        {
            // TODO: Call KeywordServiceV2.RebuildLinksAfterMoveAsync
            // For now, just log
            _logger.LogInformation("Item moved: keywords need rebuild from {Old} to {New}", oldPathIds, newPathIds);
        }

        #endregion

        #region Delete Operations

        /// <summary>
        /// Delete item and all descendants
        /// </summary>
        public async Task DeleteItemAsync(int itemId)
        {
            var item = await _context.Set<WorkspaceItemEntity>().FindAsync(itemId);
            if (item == null)
                throw new ArgumentException($"Item {itemId} not found");

            var pathIds = item.PathIds;

            // Delete all descendants
            var descendants = await _context.Set<WorkspaceItemEntity>()
                .Where(i => i.PathIds.StartsWith(pathIds))
                .ToListAsync();

            _logger.LogInformation("Deleting item {Id} and {Count} descendants", itemId, descendants.Count);

            _context.Set<WorkspaceItemEntity>().RemoveRange(descendants);
            await _context.SaveChangesAsync();

            // Delete keywords
            await OnItemDeletedAsync(pathIds);
        }

        /// <summary>
        /// Hook for keyword service to delete keywords
        /// </summary>
        private async Task OnItemDeletedAsync(string pathIds)
        {
            // TODO: Call KeywordServiceV2.DeleteKeywordsByPathAsync
            _logger.LogInformation("Item deleted: keywords need cleanup for path {Path}", pathIds);
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Get workspace item by entity type and entity ID
        /// </summary>
        public async Task<WorkspaceItemEntity?> GetWorkspaceItemByEntityAsync(byte entityType, int entityId)
        {
            return await _context.Set<WorkspaceItemEntity>()
                .FirstOrDefaultAsync(i => i.EntityType == entityType && i.EntityId == entityId);
        }

        /// <summary>
        /// Get workspace item by ID
        /// </summary>
        public async Task<WorkspaceItemEntity?> GetWorkspaceItemByIdAsync(int itemId)
        {
            return await _context.Set<WorkspaceItemEntity>()
                .FindAsync(itemId);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generate URL-friendly slug from name
        /// </summary>
        private string GenerateSlug(string name)
        {
            // Simple slug generation (can be improved)
            return name.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("/", "-")
                .Replace("'", "")
                .Replace("\"", "");
        }

        #endregion
    }
}
