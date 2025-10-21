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
        /// Removes an item from a workspace
        /// </summary>
        public async Task<bool> RemoveItemFromWorkspaceAsync(int itemId)
        {
            _logger.LogInformation("Removing item {ItemId} from workspace", itemId);

            var item = await _context.WorkspaceItems.FindAsync(itemId);
            if (item == null)
            {
                return false;
            }

            _context.WorkspaceItems.Remove(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully removed item {ItemId}", itemId);
            return true;
        }
    }
}
