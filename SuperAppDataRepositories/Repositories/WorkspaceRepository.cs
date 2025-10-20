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
    }
}
