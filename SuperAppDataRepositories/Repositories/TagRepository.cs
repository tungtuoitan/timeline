using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Extensions;
using SuperAppDataRepositories.Data;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class TagRepository : BaseRepository, ITagRepository
    {
        private readonly ApplicationDbContext _context;

        public TagRepository(
            ApplicationDbContext context,
            ILogger<TagRepository> logger, 
            IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
            _context = context;
        }

        // ✅ Refactored to EF Core - Simple query with standard filtering
        public async Task<List<Tag>> GetTags(int userId)
        {
            _logger.LogInformation("Getting tags for userId: {UserId} using EF Core", userId);

            return await _context.Tags
                .AsNoTracking() // Read-only query for better performance
                .Where(t => t.UserId == userId && t.DeletedAt == null) // Filter by user and not deleted
                .OrderBy(t => t.Name) // Order alphabetically by name
                .ToListAsync();
        }

        // ✅ Refactored to EF Core - Simple CRUD operation
        public async Task<Tag?> GetTagById(int tagId)
        {
            _logger.LogInformation("Getting tag by ID: {TagId} using EF Core", tagId);
            
            return await _context.Tags
                .AsNoTracking() // Read-only query for better performance
                .FirstOrDefaultAsync(t => t.TagId == tagId);
        }

        // ✅ Refactored to EF Core - Simple CRUD operation
        public async Task<Tag> CreateTagAsync(Tag tag)
        {
            try
            {
                _logger.LogInformation("Creating new tag with name: {Name} for user: {UserId} using EF Core", tag.Name, tag.UserId);

                // Set timestamps
                tag.CreatedAt = DateTime.UtcNow;
                tag.UpdatedAt = DateTime.UtcNow;

                // Add tag to context
                _context.Tags.Add(tag);
                
                // Save changes to database
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created tag with ID: {TagId}", tag.TagId);
                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating tag with name: {Name}", tag.Name);
                throw;
            }
        }

        // ✅ Refactored to EF Core - Simple CRUD operation
        public async Task<Tag> UpdateTagAsync(Tag tag)
        {
            try
            {
                _logger.LogInformation("Updating tag with ID: {TagId} using EF Core", tag.TagId);

                if (tag.TagId <= 0)
                {
                    throw new ArgumentException("Tag ID must be greater than 0 for updates", nameof(tag));
                }

                // Check if tag exists
                var existingTag = await _context.Tags.FindAsync(tag.TagId);
                if (existingTag == null)
                {
                    throw new InvalidOperationException($"Tag with ID {tag.TagId} not found");
                }

                // Update properties
                existingTag.Name = tag.Name;
                existingTag.Slug = tag.Slug;
                existingTag.Color = tag.Color;
                existingTag.Icon = tag.Icon;
                existingTag.Description = tag.Description;
                existingTag.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated tag with ID: {TagId}", tag.TagId);
                return existingTag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating tag with ID: {TagId}", tag.TagId);
                throw;
            }
        }

        // ✅ Refactored to EF Core - Simple CRUD operation (soft delete)
        public async Task<bool> DeleteTagAsync(int tagId)
        {
            try
            {
                _logger.LogInformation("Deleting tag with ID: {TagId} using EF Core", tagId);

                if (tagId <= 0)
                {
                    throw new ArgumentException("Tag ID must be greater than 0", nameof(tagId));
                }

                // Find tag
                var tag = await _context.Tags.FindAsync(tagId);
                if (tag == null)
                {
                    throw new InvalidOperationException($"Tag with ID {tagId} not found");
                }

                // Soft delete
                tag.DeletedAt = DateTime.UtcNow;
                tag.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted tag with ID: {TagId}", tagId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting tag with ID: {TagId}", tagId);
                throw;
            }
        }

        /// <summary>
        /// Gets all tags associated with a specific note through workspace_items
        /// REFACTORED: Migrated from stored procedure to EF Core for type safety
        /// </summary>
        public async Task<List<Tag>> GetTagsByNoteId(int noteId)
        {
            _logger.LogInformation("Getting tags for note {NoteId} using EF Core", noteId);

            // Query workspace_items where ChildType='note' and ChildId=noteId
            // Then get the parent tags
            return await _context.WorkspaceItems
                .AsNoTracking()
                .Where(wi => wi.ChildType == "note" && wi.ChildId == noteId && wi.DeletedAt == null)
                .Where(wi => wi.ParentTagId != null) // Must have parent tag
                .Include(wi => wi.ParentTag)
                .Select(wi => wi.ParentTag)
                .Distinct() // In case note appears in multiple workspaces with same parent tag
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all notes tagged with a specific tag through workspace_items
        /// REFACTORED: Migrated from stored procedure to EF Core for type safety and consistency
        /// </summary>
        public async Task<List<Note>> GetNotesByTagId(int tagId)
        {
            _logger.LogInformation("Getting notes for tag {TagId} using EF Core", tagId);

            // Query workspace_items where ParentTagId=tagId and ChildType='note'
            // Then get the child notes
            return await _context.WorkspaceItems
                .AsNoTracking()
                .Where(wi => wi.ParentTagId == tagId && wi.ChildType == "note" && wi.DeletedAt == null)
                .Include(wi => wi.ChildNote)
                .Select(wi => wi.ChildNote!)
                .Distinct() // In case note appears in multiple workspaces under same tag
                .Where(n => n.DeletedAt == null) // Filter soft-deleted notes
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AddNoteTagAsync(int noteId, int tagId, string createdBy)
        {
            try
            {
                _logger.LogInformation("Adding tag {TagId} to note {NoteId}", tagId, noteId);

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertNoteTag,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        command.Parameters.Add(new SqlParameter("@iv_TagId", tagId));
                        AddParameterIfNotNull(command, "@iv_CreatedBy", createdBy);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error adding note-tag association: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to add note-tag association: {errorMessage}");
                }

                _logger.LogInformation("Successfully added tag {TagId} to note {NoteId}", tagId, noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding tag {TagId} to note {NoteId}", tagId, noteId);
                throw;
            }
        }

        public async Task<bool> RemoveNoteTagAsync(int noteId, int tagId)
        {
            try
            {
                _logger.LogInformation("Removing tag {TagId} from note {NoteId}", tagId, noteId);

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spDeleteNoteTag,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        command.Parameters.Add(new SqlParameter("@iv_TagId", tagId));
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error removing note-tag association: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to remove note-tag association: {errorMessage}");
                }

                _logger.LogInformation("Successfully removed tag {TagId} from note {NoteId}", tagId, noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing tag {TagId} from note {NoteId}", tagId, noteId);
                throw;
            }
        }

        public async Task<bool> RemoveAllNoteTagsAsync(int noteId)
        {
            try
            {
                _logger.LogInformation("Removing all tags from note {NoteId}", noteId);

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spDeleteNoteTagsByNoteId,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error removing all note tags: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to remove all note tags: {errorMessage}");
                }

                _logger.LogInformation("Successfully removed all tags from note {NoteId}", noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing all tags from note {NoteId}", noteId);
                throw;
            }
        }

        public async Task<List<TagTree>> GetTagTreeAsync(int workspaceId, int userId)
        {
            try
            {
                _logger.LogInformation("Getting tag tree for workspaceId: {WorkspaceId}, userId: {UserId}", 
                    workspaceId, userId);

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectTagTreeWithSharing,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@workspace_id", workspaceId));
                        command.Parameters.Add(new SqlParameter("@user_id", userId));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToTagTreeListAsync,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tag tree for workspaceId: {WorkspaceId}, userId: {UserId}", workspaceId, userId);
                throw;
            }
        }

        public async Task<List<TagTree>> GetWorkspaceTagTreeAsync(int workspaceId, int userId)
        {
            try
            {
                _logger.LogInformation("Getting workspace tag tree for workspaceId: {WorkspaceId}, userId: {UserId}", 
                    workspaceId, userId);

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectWorkspaceTagTree,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@workspace_id", workspaceId));
                        command.Parameters.Add(new SqlParameter("@user_id", userId));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToTagTreeListAsync,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspace tag tree for workspaceId: {WorkspaceId}, userId: {UserId}", workspaceId, userId);
                throw;
            }
        }
    }
}