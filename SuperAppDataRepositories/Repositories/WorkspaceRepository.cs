using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Extensions;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class WorkspaceRepository : BaseRepository, IWorkspaceRepository
    {
        private readonly ApplicationDbContext _context;

        public WorkspaceRepository(
            ILogger<WorkspaceRepository> logger, 
            IConnectionFactory connectionFactory,
            ApplicationDbContext context)
            : base(connectionFactory, logger)
        {
            _context = context;
        }

        /// <summary>
        /// Get workspace by ID using EF Core
        /// Filters by user_id to ensure user has access
        /// </summary>
        public async Task<Workspace?> GetWorkspaceByIdAsync(int workspaceId, int userId)
        {
            _logger.LogInformation("Getting workspace {WorkspaceId} for user {UserId} using EF Core", 
                workspaceId, userId);

            return await _context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => 
                    w.WorkspaceId == workspaceId && 
                    w.UserId == userId &&
                    w.DeletedAt == null);
        }

        public async Task<List<Workspace>> GetUserWorkspacesAsync(int userId, bool includeArchived = false)
        {
            _logger.LogInformation("Getting workspaces for user {UserId}, includeArchived: {IncludeArchived}", 
                userId, includeArchived);

            return await ExecuteStoredProcedureAsync<List<Workspace>>(
                StoredProcedures.spSelectUserWorkspaces,
                addParameters: async (command) =>
                {
                    command.Parameters.Add(new SqlParameter("@user_id", userId));
                    command.Parameters.Add(new SqlParameter("@include_archived", includeArchived));
                    await Task.CompletedTask;
                },
                mapResult: MapToWorkspaceListAsync,
                useSuperAppConnection: true
            );
        }

        /// <summary>
        /// Maps SqlDataReader to a list of Workspace objects
        /// Used for stored procedure that returns workspace list
        /// </summary>
        private async Task<List<Workspace>> MapToWorkspaceListAsync(SqlDataReader reader)
        {
            var workspaces = new List<Workspace>();
            while (await reader.ReadAsync())
            {
                workspaces.Add(MapWorkspaceFromReader(reader));
            }
            return workspaces;
        }

        /// <summary>
        /// Maps a single row from SqlDataReader to Workspace object
        /// Handles lowercase column names from database
        /// </summary>
        private Workspace MapWorkspaceFromReader(SqlDataReader reader)
        {
            return new Workspace
            {
                WorkspaceId = reader.GetInt32("workspace_id"),
                UserId = reader.GetInt32("user_id"),
                Name = reader.GetString("name"),
                Description = reader.GetNullableString("description"),
                Color = reader.GetNullableString("color"),
                Icon = reader.GetNullableString("icon"),
                Type = reader.GetNullableString("type") ?? "hierarchy",
                MaxDepth = reader.GetNullableInt32("max_depth") ?? 10,
                IsDefault = reader.GetBoolean("is_default"),
                IsPublic = reader.GetBoolean("is_public"),
                IsTemplate = reader.GetBoolean("is_template"),
                IsArchived = reader.GetBoolean("is_archived"),
                TagCount = reader.GetInt32("tag_count"),
                RelationshipCount = reader.GetInt32("relationship_count"),
                MemberCount = reader.GetInt32("member_count"),
                Settings = reader.GetNullableString("settings"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetNullableDateTime("updated_at"),
                LastAccessedAt = reader.GetNullableDateTime("last_accessed_at"),
                DeletedAt = reader.GetNullableDateTime("deleted_at")
            };
        }

        /// <summary>
        /// Validates that a user has the required access level to a workspace
        /// </summary>
        public async Task ValidateUserAccessAsync(int workspaceId, int userId, string[] requiredRoles)
        {
            _logger.LogInformation(
                "Validating user {UserId} access to workspace {WorkspaceId} with roles: {Roles}",
                userId, workspaceId, string.Join(", ", requiredRoles));

            // Check if user is workspace owner
            var workspace = await _context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId && w.DeletedAt == null);

            if (workspace == null)
            {
                throw new ArgumentException($"Workspace {workspaceId} not found");
            }

            // Owner always has access
            if (workspace.UserId == userId)
            {
                _logger.LogInformation("User {UserId} is owner of workspace {WorkspaceId}", userId, workspaceId);
                return;
            }

            // Check workspace_members table for access
            var member = await _context.WorkspaceMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => 
                    m.WorkspaceId == workspaceId && 
                    m.UserId == userId &&
                    m.DeletedAt == null);

            if (member == null)
            {
                throw new UnauthorizedAccessException(
                    $"User {userId} does not have access to workspace {workspaceId}");
            }

            // Check if user's role is in required roles
            if (!requiredRoles.Contains(member.Role.ToLower()))
            {
                throw new UnauthorizedAccessException(
                    $"User {userId} has role '{member.Role}' but requires one of: {string.Join(", ", requiredRoles)}");
            }

            _logger.LogInformation(
                "User {UserId} has valid access to workspace {WorkspaceId} with role '{Role}'",
                userId, workspaceId, member.Role);
        }

        /// <summary>
        /// Adds an item (tag or note) to a workspace using EF Core
        /// </summary>
        public async Task<WorkspaceItem> AddItemToWorkspaceAsync(WorkspaceItem item)
        {
            _logger.LogInformation(
                "Adding item to workspace {WorkspaceId}: ParentTagId={ParentTagId}, ChildType={ChildType}, ChildId={ChildId}",
                item.WorkspaceId, item.ParentTagId, item.ChildType, item.ChildId);

            // Check if item already exists in workspace (duplicate prevention)
            var exists = await ItemExistsAsync(
                item.WorkspaceId, 
                item.ParentTagId, 
                item.ChildType, 
                item.ChildId);

            if (exists)
            {
                var parentInfo = item.ParentTagId.HasValue ? $"parent tag {item.ParentTagId}" : "root";
                var message = $"Item already exists in workspace: {item.ChildType} with ID {item.ChildId} " +
                              $"under {parentInfo} in workspace {item.WorkspaceId}";
                
                _logger.LogWarning(
                    "Duplicate item rejected: WorkspaceId={WorkspaceId}, ParentTagId={ParentTagId}, " +
                    "ChildType={ChildType}, ChildId={ChildId}",
                    item.WorkspaceId, item.ParentTagId, item.ChildType, item.ChildId);
                
                throw new ArgumentException(message);
            }

            // Validate child entity exists
            if (item.ChildType.ToLower() == "tag")
            {
                var tagExists = await _context.Tags.AnyAsync(t => t.TagId == item.ChildId && t.DeletedAt == null);
                if (!tagExists)
                {
                    throw new ArgumentException($"Tag with ID {item.ChildId} not found");
                }
            }
            else if (item.ChildType.ToLower() == "note")
            {
                var noteExists = await _context.Notes.AnyAsync(n => n.NoteId == item.ChildId && n.DeletedAt == null);
                if (!noteExists)
                {
                    throw new ArgumentException($"Note with ID {item.ChildId} not found");
                }
            }

            // Validate parent tag exists if specified
            if (item.ParentTagId.HasValue)
            {
                var parentExists = await _context.Tags.AnyAsync(t => t.TagId == item.ParentTagId && t.DeletedAt == null);
                if (!parentExists)
                {
                    throw new ArgumentException($"Parent tag with ID {item.ParentTagId} not found");
                }
            }

            // Add item to workspace
            _context.WorkspaceItems.Add(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully added item {ItemId} to workspace {WorkspaceId}",
                item.ItemId, item.WorkspaceId);

            return item;
        }

        /// <summary>
        /// Checks if a workspace item already exists
        /// </summary>
        public async Task<bool> ItemExistsAsync(int workspaceId, int? parentTagId, string childType, int childId)
        {
            return await _context.WorkspaceItems
                .AsNoTracking()
                .AnyAsync(i => 
                    i.WorkspaceId == workspaceId &&
                    i.ParentTagId == parentTagId &&
                    i.ChildType.ToLower() == childType.ToLower() &&
                    i.ChildId == childId);
        }

        /// <summary>
        /// Gets all items in a workspace
        /// </summary>
        public async Task<List<WorkspaceItem>> GetWorkspaceItemsAsync(int workspaceId)
        {
            _logger.LogInformation("Getting all items for workspace {WorkspaceId}", workspaceId);

            return await _context.WorkspaceItems
                .AsNoTracking()
                .Where(i => i.WorkspaceId == workspaceId)
                .Include(i => i.ParentTag)
                .Include(i => i.ChildTag)
                .Include(i => i.AddedByUser)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Removes an item from a workspace (soft delete)
        /// Also removes all descendants (children, grandchildren, etc.)
        /// </summary>
        public async Task<bool> RemoveItemFromWorkspaceAsync(int itemId, bool deleteDescendants = true)
        {
            _logger.LogInformation("Removing item {ItemId} from workspace (deleteDescendants: {DeleteDescendants})",
                itemId, deleteDescendants);

            var item = await _context.WorkspaceItems
                .Where(i => i.ItemId == itemId && i.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (item == null)
            {
                _logger.LogWarning("Item {ItemId} not found or already deleted", itemId);
                return false;
            }

            // Soft delete the item
            item.DeletedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            // If deleteDescendants, also soft delete all children using item_path
            if (deleteDescendants && !string.IsNullOrEmpty(item.ItemPath))
            {
                var descendants = await _context.WorkspaceItems
                    .Where(i => i.WorkspaceId == item.WorkspaceId
                        && i.ItemPath.StartsWith(item.ItemPath)
                        && i.ItemId != itemId
                        && i.DeletedAt == null)
                    .ToListAsync();

                foreach (var descendant in descendants)
                {
                    descendant.DeletedAt = DateTime.UtcNow;
                    descendant.UpdatedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Soft deleted {Count} descendants of item {ItemId}",
                    descendants.Count, itemId);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully soft deleted item {ItemId}", itemId);
            return true;
        }

        /// <summary>
        /// Gets the complete workspace tree (tags, notes, and files) with hierarchy
        /// This is the new unified tree endpoint supporting all entity types
        /// </summary>
        public async Task<List<WorkspaceTreeItem>> GetWorkspaceTreeAsync(int workspaceId, int userId)
        {
            try
            {
                _logger.LogInformation(
                    "Getting workspace tree for workspace {WorkspaceId}, user {UserId}",
                    workspaceId, userId);

                // Validate user access
                await ValidateUserAccessAsync(workspaceId, userId, new[] { "owner", "editor", "viewer" });

                // Get all workspace items (without Include - we'll load related data separately)
                var items = await _context.WorkspaceItems
                    .AsNoTracking()
                    .Where(i => i.WorkspaceId == workspaceId)
                    .OrderBy(i => i.SortOrder)
                    .ToListAsync();

                // Get all tag IDs and note IDs
                var tagIds = items.Where(i => i.ChildType.ToLower() == "tag").Select(i => i.ChildId).Distinct().ToList();
                var noteIds = items.Where(i => i.ChildType.ToLower() == "note").Select(i => i.ChildId).Distinct().ToList();

                // Load tags and notes in bulk
                var tags = await _context.Tags
                    .AsNoTracking()
                    .Where(t => tagIds.Contains(t.TagId))
                    .ToDictionaryAsync(t => t.TagId, t => t);

                var notes = await _context.Notes
                    .AsNoTracking()
                    .Where(n => noteIds.Contains(n.NoteId))
                    .ToDictionaryAsync(n => n.NoteId, n => n);

                var treeItems = new List<WorkspaceTreeItem>();

                foreach (var item in items)
                {
                    var treeItem = new WorkspaceTreeItem
                    {
                        ItemType = item.ChildType,
                        ItemId = item.ItemId,        // workspace_items.item_id
                        ChildId = item.ChildId,      // actual tag_id/note_id
                        ParentId = item.ParentTagId,
                        Level = item.Depth,
                        Position = item.SortOrder,
                        CreatedAt = item.CreatedAt ?? DateTime.UtcNow
                    };

                    // Populate fields based on child type
                    if (item.ChildType.ToLower() == "tag" && tags.TryGetValue(item.ChildId, out var tag))
                    {
                        treeItem.UserId = tag.UserId;
                        treeItem.Name = tag.Name;
                        treeItem.Slug = tag.Slug;
                        treeItem.Color = tag.Color;
                        treeItem.Icon = tag.Icon;
                        treeItem.UpdatedAt = tag.UpdatedAt;
                        treeItem.AccessType = tag.UserId == userId ? "owner" : "shared";
                    }
                    else if (item.ChildType.ToLower() == "note" && notes.TryGetValue(item.ChildId, out var note))
                    {
                        treeItem.UserId = note.UserId;
                        treeItem.Name = note.Name;
                        treeItem.UpdatedAt = note.UpdatedAt;
                        treeItem.AccessType = note.UserId == userId ? "owner" : "shared";
                    }

                    treeItems.Add(treeItem);
                }

                _logger.LogInformation(
                    "Successfully retrieved {ItemCount} items for workspace {WorkspaceId}",
                    treeItems.Count, workspaceId);

                return treeItems;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized access attempt to workspace {WorkspaceId} by user {UserId}",
                    workspaceId, userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting workspace tree for workspace {WorkspaceId}, user {UserId}",
                    workspaceId, userId);
                throw;
            }
        }

        /// <summary>
        /// Gets the workspace tag tree (backward compatibility - tags only)
        /// DEPRECATED: Use GetWorkspaceTreeAsync for new development
        /// </summary>
        [Obsolete("Use GetWorkspaceTreeAsync instead. This method will be removed in v2.0")]
        public async Task<List<WorkspaceTagTree>> GetWorkspaceTagTreeAsync(int workspaceId, int userId)
        {
            _logger.LogWarning(
                "Using deprecated GetWorkspaceTagTreeAsync. Migrate to GetWorkspaceTreeAsync. " +
                "WorkspaceId={WorkspaceId}, UserId={UserId}",
                workspaceId, userId);

            // Validate user access
            await ValidateUserAccessAsync(workspaceId, userId, new[] { "owner", "editor", "viewer" });

            // Get only tag items
            var items = await _context.WorkspaceItems
                .AsNoTracking()
                .Where(i => i.WorkspaceId == workspaceId && i.ChildType.ToLower() == "tag")
                .Include(i => i.ChildTag)
                .OrderBy(i => i.SortOrder)
                .ToListAsync();

            var tagTree = new List<WorkspaceTagTree>();

            foreach (var item in items)
            {
                if (item.ChildTag == null) continue;

                tagTree.Add(new WorkspaceTagTree
                {
                    TagId = item.ChildTag.TagId,
                    UserId = item.ChildTag.UserId,
                    Name = item.ChildTag.Name,
                    ParentId = item.ParentTagId,
                    Slug = item.ChildTag.Slug,
                    Color = item.ChildTag.Color,
                    Icon = item.ChildTag.Icon,
                    Level = item.Depth,
                    Position = item.SortOrder,
                    AccessType = item.ChildTag.UserId == userId ? "owner" : "shared",
                    CreatedAt = item.ChildTag.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = item.ChildTag.UpdatedAt
                });
            }

            return tagTree;
        }

        /// <summary>
        /// Updates workspace item metadata using stored procedure
        /// </summary>
        public async Task<WorkspaceItem> UpdateWorkspaceItemAsync(
            long itemId,
            int userId,
            string? label = null,
            string? notes = null,
            string? color = null,
            string? icon = null,
            int? sortOrder = null)
        {
            _logger.LogInformation(
                "Updating workspace item {ItemId} for user {UserId}: Label={Label}, Notes={Notes}, Color={Color}, Icon={Icon}, SortOrder={SortOrder}",
                itemId, userId, label, notes, color, icon, sortOrder);

            return await ExecuteStoredProcedureAsync<WorkspaceItem>(
                "usp_update_workspace_item",
                addParameters: async (command) =>
                {
                    command.Parameters.Add(new SqlParameter("@item_id", itemId));
                    command.Parameters.Add(new SqlParameter("@user_id", userId));
                    
                    // Add optional parameters
                    if (label != null)
                        command.Parameters.Add(new SqlParameter("@label", label));
                    if (notes != null)
                        command.Parameters.Add(new SqlParameter("@notes", notes));
                    if (color != null)
                        command.Parameters.Add(new SqlParameter("@color", color));
                    if (icon != null)
                        command.Parameters.Add(new SqlParameter("@icon", icon));
                    if (sortOrder.HasValue)
                        command.Parameters.Add(new SqlParameter("@sort_order", sortOrder.Value));
                    
                    await Task.CompletedTask;
                },
                mapResult: MapToWorkspaceItemAsync,
                useSuperAppConnection: true
            );
        }

        /// <summary>
        /// Moves workspace item to a different parent using stored procedure
        /// </summary>
        public async Task<WorkspaceItem> MoveWorkspaceItemAsync(
            long itemId,
            int userId,
            int? newParentTagId,
            int? sortOrder = null)
        {
            _logger.LogInformation(
                "Moving workspace item {ItemId} for user {UserId}: NewParentTagId={NewParentTagId}, SortOrder={SortOrder}",
                itemId, userId, newParentTagId, sortOrder);

            return await ExecuteStoredProcedureAsync<WorkspaceItem>(
                StoredProcedures.spMoveItem,
                addParameters: async (command) =>
                {
                    command.Parameters.Add(new SqlParameter("@item_id", itemId));
                    command.Parameters.Add(new SqlParameter("@user_id", userId));
                    command.Parameters.Add(new SqlParameter("@new_parent_tag_id", 
                        (object?)newParentTagId ?? DBNull.Value));
                    
                    if (sortOrder.HasValue)
                        command.Parameters.Add(new SqlParameter("@sort_order", sortOrder.Value));
                    
                    await Task.CompletedTask;
                },
                mapResult: MapToWorkspaceItemAsync,
                useSuperAppConnection: true
            );
        }

        /// <summary>
        /// Maps SqlDataReader to a single WorkspaceItem object
        /// </summary>
        private async Task<WorkspaceItem> MapToWorkspaceItemAsync(SqlDataReader reader)
        {
            if (!await reader.ReadAsync())
            {
                throw new KeyNotFoundException("Workspace item not found");
            }

            return new WorkspaceItem
            {
                ItemId = reader.GetInt64("item_id"),
                WorkspaceId = reader.GetInt32("workspace_id"),
                ParentTagId = reader.GetNullableInt32("parent_tag_id"),
                ChildType = reader.GetString("child_type"),
                ChildId = reader.GetInt32("child_id"),
                Label = reader.GetNullableString("label"),
                Notes = reader.GetNullableString("notes"),
                Color = reader.GetNullableString("color"),
                Icon = reader.GetNullableString("icon"),
                SortOrder = reader.GetInt32("sort_order"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetNullableDateTime("updated_at"),
                AddedBy = reader.GetInt32("added_by")
            };
        }
    }
}
