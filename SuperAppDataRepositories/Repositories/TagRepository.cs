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

        /// <summary>
        /// Adds a tag association to a note using EF Core
        /// REFACTORED: Migrated from stored procedure to EF Core for consistency
        /// </summary>
        public async Task<bool> AddNoteTagAsync(int noteId, int tagId, string createdBy)
        {
            try
            {
                _logger.LogInformation("Adding tag {TagId} to note {NoteId} using EF Core", tagId, noteId);

                // Check if association already exists
                var exists = await _context.NoteTags
                    .AnyAsync(nt => nt.NoteId == noteId && nt.TagId == tagId);

                if (exists)
                {
                    _logger.LogWarning("Tag {TagId} is already associated with note {NoteId}", tagId, noteId);
                    return true; // Already exists, consider it success
                }

                // Validate note exists
                var noteExists = await _context.Notes.AnyAsync(n => n.NoteId == noteId && n.DeletedAt == null);
                if (!noteExists)
                {
                    throw new InvalidOperationException($"Note with ID {noteId} not found");
                }

                // Validate tag exists
                var tagExists = await _context.Tags.AnyAsync(t => t.TagId == tagId && t.DeletedAt == null);
                if (!tagExists)
                {
                    throw new InvalidOperationException($"Tag with ID {tagId} not found");
                }

                // Create new association
                var noteTag = new NoteTag
                {
                    NoteId = noteId,
                    TagId = tagId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.NoteTags.Add(noteTag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully added tag {TagId} to note {NoteId} using EF Core", tagId, noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding tag {TagId} to note {NoteId}", tagId, noteId);
                throw;
            }
        }

        /// <summary>
        /// Removes a tag association from a note using EF Core
        /// REFACTORED: Migrated from stored procedure to EF Core for consistency
        /// </summary>
        public async Task<bool> RemoveNoteTagAsync(int noteId, int tagId)
        {
            try
            {
                _logger.LogInformation("Removing tag {TagId} from note {NoteId} using EF Core", tagId, noteId);

                // Find the association
                var noteTag = await _context.NoteTags
                    .FirstOrDefaultAsync(nt => nt.NoteId == noteId && nt.TagId == tagId);

                if (noteTag == null)
                {
                    _logger.LogWarning("Tag {TagId} is not associated with note {NoteId}", tagId, noteId);
                    return false;
                }

                // Remove the association
                _context.NoteTags.Remove(noteTag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully removed tag {TagId} from note {NoteId} using EF Core", tagId, noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing tag {TagId} from note {NoteId}", tagId, noteId);
                throw;
            }
        }

        /// <summary>
        /// Removes all tag associations from a note using EF Core
        /// REFACTORED: Migrated from stored procedure to EF Core for consistency
        /// </summary>
        public async Task<bool> RemoveAllNoteTagsAsync(int noteId)
        {
            try
            {
                _logger.LogInformation("Removing all tags from note {NoteId} using EF Core", noteId);

                // Find all associations for this note
                var noteTags = await _context.NoteTags
                    .Where(nt => nt.NoteId == noteId)
                    .ToListAsync();

                if (!noteTags.Any())
                {
                    _logger.LogInformation("No tags found for note {NoteId}", noteId);
                    return true;
                }

                // Remove all associations
                _context.NoteTags.RemoveRange(noteTags);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully removed {Count} tags from note {NoteId} using EF Core",
                    noteTags.Count, noteId);
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

        /// <summary>
        /// Batch move multiple tags to a new parent/position in workspace
        /// Works with workspace_items table to manage hierarchy
        /// Uses execution strategy to handle transactions with SQL Server retry logic
        /// </summary>
        public async Task BatchMoveTagsAsync(int[] tagIds, int? newParentId, int startIndex, int userId)
        {
            // Use execution strategy for retry-compatible transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation(
                        "Batch moving {Count} tags to parent {ParentId} at index {StartIndex} for user {UserId}",
                        tagIds.Length,
                        newParentId ?? 0,
                        startIndex,
                        userId);

                    // 1. Validate all tags exist and belong to the user
                    var tags = await _context.Tags
                        .Where(t => tagIds.Contains(t.TagId) && t.UserId == userId && t.DeletedAt == null)
                        .ToListAsync();

                    if (tags.Count != tagIds.Length)
                    {
                        var foundIds = tags.Select(t => t.TagId).ToList();
                        var missingIds = tagIds.Except(foundIds).ToList();
                        throw new InvalidOperationException(
                            $"One or more tags not found or not accessible. Missing IDs: {string.Join(", ", missingIds)}");
                    }

                    // 2. Get workspace_items for these tags
                    var workspaceItems = await _context.WorkspaceItems
                        .Where(wi => tagIds.Contains(wi.ChildId) && wi.ChildType == "tag" && wi.DeletedAt == null)
                        .ToListAsync();

                    if (workspaceItems.Count == 0)
                    {
                        throw new InvalidOperationException("No workspace items found for the specified tags");
                    }

                    // CRITICAL: Ensure ALL tags have workspace_items entries
                    if (workspaceItems.Count != tagIds.Length)
                    {
                        var foundTagIds = workspaceItems.Select(wi => wi.ChildId).ToList();
                        var missingTagIds = tagIds.Except(foundTagIds).ToList();
                        throw new InvalidOperationException(
                            $"Some tags don't have workspace_items entries. Missing tag IDs: {string.Join(", ", missingTagIds)}");
                    }

                    // Get workspace_id from first item (all should be in same workspace)
                    var workspaceId = workspaceItems.First().WorkspaceId;

                    // 3. Validate new parent exists if specified
                    if (newParentId.HasValue && newParentId.Value > 0)
                    {
                        var newParent = await _context.Tags
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.TagId == newParentId.Value && t.UserId == userId && t.DeletedAt == null);

                        if (newParent == null)
                        {
                            throw new InvalidOperationException($"New parent tag with ID {newParentId.Value} not found or not accessible");
                        }

                        // Check circular dependencies - prevent moving a parent into its own children
                        foreach (var tagId in tagIds)
                        {
                            if (await IsDescendantInWorkspaceAsync(workspaceId, newParentId.Value, tagId))
                            {
                                throw new InvalidOperationException(
                                    $"Cannot move tag {tagId} - circular dependency detected. " +
                                    $"Tag {newParentId.Value} is a descendant of tag {tagId}");
                            }
                        }
                    }

                    // 4. Update all workspace_items in the specified order
                    int currentSortOrder = startIndex;
                    foreach (var tagId in tagIds)
                    {
                        var item = workspaceItems.FirstOrDefault(wi => wi.ChildId == tagId);
                        if (item != null)
                        {
                            // Update parent and sort order
                            item.ParentTagId = newParentId;
                            item.SortOrder = currentSortOrder++;
                            item.UpdatedAt = DateTime.UtcNow;

                            _logger.LogDebug("Moving workspace item {ItemId} (tag {TagId}) to parent {ParentId} at sort order {SortOrder}",
                                item.ItemId, tagId, newParentId ?? 0, item.SortOrder);
                        }
                    }

                    // 5. Save all changes
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Successfully batch moved {Count} tags for user {UserId}",
                        tagIds.Length,
                        userId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during batch move, transaction rolled back. UserId: {UserId}", userId);
                    throw;
                }
            });
        }

        /// <summary>
        /// Helper method to check if targetTagId is a descendant of potentialParentTagId in workspace hierarchy
        /// Used to prevent circular dependencies when moving tags
        /// </summary>
        private async Task<bool> IsDescendantInWorkspaceAsync(int workspaceId, int targetTagId, int potentialParentTagId)
        {
            // Get all workspace_items for efficient in-memory traversal
            var allItems = await _context.WorkspaceItems
                .AsNoTracking()
                .Where(wi => wi.WorkspaceId == workspaceId && wi.ChildType == "tag" && wi.DeletedAt == null)
                .Select(wi => new { wi.ChildId, wi.ParentTagId })
                .ToListAsync();

            // Build parent-child map
            var childrenMap = allItems
                .Where(wi => wi.ParentTagId.HasValue)
                .GroupBy(wi => wi.ParentTagId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(wi => wi.ChildId).ToList());

            // Recursive search
            bool CheckDescendants(int currentTagId)
            {
                if (currentTagId == targetTagId)
                    return true;

                if (!childrenMap.ContainsKey(currentTagId))
                    return false;

                foreach (var childId in childrenMap[currentTagId])
                {
                    if (CheckDescendants(childId))
                        return true;
                }

                return false;
            }

            return CheckDescendants(potentialParentTagId);
        }
    }
}