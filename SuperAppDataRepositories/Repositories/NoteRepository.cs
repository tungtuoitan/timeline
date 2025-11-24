using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Extensions;
using SuperAppDataRepositories.Data;
using SuperAppModels.Models;
using SuperAppModels.DTOs;

namespace SuperAppDataRepositories.Repositories
{
    public class NoteRepository : BaseRepository, INoteRepository
    {
        private readonly ApplicationDbContext _context;

        public NoteRepository(
            ApplicationDbContext context,
            ILogger<NoteRepository> logger, 
            IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves notes with filtering using EF Core (hybrid approach)
        /// </summary>
        public async Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, List<int>? tagIds = null, int? createdByUserId = null)
        {
            try
            {
                var query = _context.Notes
                    .AsNoTracking() // Read-only query for better performance
                    .Where(n => n.DeletedAt == null); // Soft delete filter

                // Apply filters based on parameters
                if (!getAll && createdByUserId.HasValue)
                {
                    query = query.Where(n => n.UserId == createdByUserId.Value);
                }

                if (!string.IsNullOrEmpty(searchText))
                {
                    query = query.Where(n => n.Name.Contains(searchText) || 
                                            (n.Content != null && n.Content.Contains(searchText)));
                }

                // Filter by tags using EF Core with workspace_items table
                if (tagIds != null && tagIds.Any())
                {
                    _logger.LogInformation("Tag filtering requested - using EF Core join with workspace_items");
                    
                    // Join with workspace_items to filter notes that are associated with specified tags
                    query = query.Where(n => _context.WorkspaceItems
                        .Where(wi => wi.ChildType == "note" 
                                  && wi.ChildId == n.NoteId 
                                  && wi.ParentTagId.HasValue
                                  && tagIds.Contains(wi.ParentTagId.Value)
                                  && wi.DeletedAt == null)
                        .Any());
                }

                // Order by creation date (most recent first)
                query = query.OrderByDescending(n => n.CreatedAt);

                var notes = await query.ToListAsync();

                _logger.LogInformation("Retrieved {Count} notes using EF Core", notes.Count);
                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving notes. Parameters: getAll={GetAll}, searchText={SearchText}, tagIds={TagIds}, createdByUserId={CreatedByUserId}", 
                    getAll, searchText, tagIds != null ? string.Join(",", tagIds) : "null", createdByUserId);
                throw;
            }
        }

        /// <summary>
        /// Maps notes with embedded JSON tags from the stored procedure result
        /// </summary>
        private async Task<List<Note>> MapNotesWithTagsAsync(SqlDataReader reader)
        {
            var notes = new List<Note>();
            
            while (await reader.ReadAsync())
            {
                var note = new Note
                {
                    NoteId = reader.GetInt32("note_id"),
                    Name = reader.GetString("name"),
                    Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                    Content = reader.IsDBNull("content") ? null : reader.GetString("content"),
                    UserId = reader.IsDBNull("user_id") ? 0 : reader.GetInt32("user_id"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at"),
                    IsArchived = reader.GetBoolean("is_archived"),
                    Slug = reader.IsDBNull("slug") ? null : reader.GetString("slug"),
                    Color = reader.IsDBNull("color") ? null : reader.GetString("color"),
                    Icon = reader.IsDBNull("icon") ? null : reader.GetString("icon"),
                    IsPinned = reader.IsDBNull("is_pinned") ? false : reader.GetBoolean("is_pinned"),
                    IsFavorite = reader.IsDBNull("is_favorite") ? false : reader.GetBoolean("is_favorite"),
                    WordCount = reader.IsDBNull("word_count") ? 0 : reader.GetInt32("word_count"),
                    VersionCount = reader.IsDBNull("version_count") ? 1 : reader.GetInt32("version_count")
                };

                // Parse TagsJSON if present (removing Tags property references for now)
                if (!reader.IsDBNull("TagsJSON"))
                {
                    var tagsJson = reader.GetString("TagsJSON");
                    if (!string.IsNullOrEmpty(tagsJson))
                    {
                        try
                        {
                            var tagData = System.Text.Json.JsonSerializer.Deserialize<List<TagJsonDto>>(tagsJson);
                            // TODO: Handle tags relationship properly once Note-Tag relationship is defined
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse TagsJSON for note {NoteId}: {TagsJson}", 
                                note.NoteId, tagsJson);
                            // Continue with empty tags list
                        }
                    }
                }

                notes.Add(note);
            }

            return notes;
        }

        /// <summary>
        /// DTO for deserializing tag JSON from stored procedure
        /// </summary>
        private class TagJsonDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }







        /// <summary>
        /// Gets a single note by ID with associated tags loaded through navigation properties
        /// REFACTORED: Migrated from stored procedure to EF Core for better type safety and consistency
        /// </summary>
        public async Task<Note?> GetNoteById(int noteId)
        {
            try
            {
                _logger.LogInformation("Getting note {NoteId} using EF Core", noteId);

                var note = await _context.Notes
                    .AsNoTracking()
                    .Where(n => n.NoteId == noteId && n.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (note == null)
                {
                    _logger.LogWarning("Note {NoteId} not found or has been deleted", noteId);
                    return null;
                }

                // Optionally load tags through workspace_items if needed
                // For now, returning note without tags to match simplified structure
                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting note with ID: {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Creates a new note using EF Core
        /// REFACTORED: Migrated from stored procedure to EF Core for simpler, type-safe operations
        /// </summary>
        public async Task<Note> CreateNoteAsync(Note note, List<int>? tagIds = null, int? createdByUserId = null)
        {
            try
            {
                _logger.LogInformation("Creating new note with title: {Title} using EF Core", note.Name);

                // Set the UserId if provided
                if (createdByUserId.HasValue)
                {
                    note.UserId = createdByUserId.Value;
                }

                // Set timestamps
                note.CreatedAt = DateTime.UtcNow;
                note.UpdatedAt = DateTime.UtcNow;

                // Add note to context
                _context.Notes.Add(note);
                await _context.SaveChangesAsync();

                // Associate tags with the note if provided
                if (tagIds != null && tagIds.Any())
                {
                    await AssociateTagsWithNoteAsync(note.NoteId, tagIds, note.UserId);
                }

                _logger.LogInformation("Successfully created note with ID: {NoteId} using EF Core", note.NoteId);
                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating note");
                throw;
            }
        }

        /// <summary>
        /// Associates tags with a note using EF Core
        /// REFACTORED: Migrated from stored procedure to EF Core for better performance and consistency
        /// </summary>
        private async Task AssociateTagsWithNoteAsync(int noteId, List<int> tagIds, int createdBy)
        {
            // Filter out invalid tag IDs before processing
            var validTagIds = tagIds?.Where(id => id > 0).ToList();
            
            if (validTagIds == null || !validTagIds.Any())
            {
                _logger.LogInformation("No valid tag IDs to associate with note {NoteId}", noteId);
                return;
            }

            _logger.LogInformation("Associating {TagCount} valid tags [{TagIds}] with note {NoteId} using EF Core", 
                validTagIds.Count, string.Join(",", validTagIds), noteId);

            try
            {
                // Create NoteTag associations in bulk
                var noteTagsToAdd = validTagIds.Select(tagId => new NoteTag
                {
                    NoteId = noteId,
                    TagId = tagId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy.ToString()
                }).ToList();

                // Add all associations at once (more efficient than one-by-one)
                await _context.NoteTags.AddRangeAsync(noteTagsToAdd);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully associated {Count} tags with note {NoteId}", 
                    noteTagsToAdd.Count, noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while associating tags with note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing note using EF Core
        /// REFACTORED: Migrated from stored procedure to EF Core for simpler, type-safe updates
        /// </summary>
        public async Task<Note> UpdateNoteAsync(Note note, List<int>? tagIds = null, int? createdByUserId = null)
        {
            try
            {
                _logger.LogInformation("Updating note with ID: {NoteId} using EF Core", note.NoteId);

                if (note.NoteId <= 0)
                {
                    throw new ArgumentException("Note ID must be greater than 0 for updates", nameof(note));
                }

                // Check if note exists and get tracking entity
                var existingNote = await _context.Notes
                    .FirstOrDefaultAsync(n => n.NoteId == note.NoteId && n.DeletedAt == null);

                if (existingNote == null)
                {
                    _logger.LogWarning("Note with ID {NoteId} not found for update", note.NoteId);
                    throw new InvalidOperationException($"Note with ID {note.NoteId} not found");
                }

                // Update properties
                existingNote.Name = note.Name;
                existingNote.Description = note.Description;
                existingNote.Content = note.Content;
                existingNote.Slug = note.Slug;
                existingNote.Color = note.Color;
                existingNote.Icon = note.Icon;
                existingNote.IsArchived = note.IsArchived;
                existingNote.IsPinned = note.IsPinned;
                existingNote.IsFavorite = note.IsFavorite;
                existingNote.UpdatedAt = DateTime.UtcNow;

                // Set UserId if provided
                if (createdByUserId.HasValue)
                {
                    existingNote.UserId = createdByUserId.Value;
                }

                // Save changes to database
                await _context.SaveChangesAsync();

                // Update tag associations if provided
                if (tagIds != null)
                {
                    // Remove all existing tag associations
                    await RemoveAllTagAssociationsAsync(existingNote.NoteId);
                    
                    // Add new tag associations
                    if (tagIds.Any())
                    {
                        await AssociateTagsWithNoteAsync(existingNote.NoteId, tagIds, existingNote.UserId);
                    }
                }

                _logger.LogInformation("Successfully updated note with ID: {NoteId} using EF Core", 
                    existingNote.NoteId);
                
                return existingNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating note with ID: {NoteId}", note.NoteId);
                throw;
            }
        }

        /// <summary>
        /// Removes all tag associations for a note using EF Core
        /// REFACTORED: Migrated from stored procedure to EF Core for consistency with other operations
        /// </summary>
        private async Task RemoveAllTagAssociationsAsync(int noteId)
        {
            try
            {
                _logger.LogInformation("Removing all tag associations for note {NoteId} using EF Core", noteId);

                // Find all note-tag associations
                var noteTagsToRemove = await _context.NoteTags
                    .Where(nt => nt.NoteId == noteId)
                    .ToListAsync();

                if (noteTagsToRemove.Any())
                {
                    _logger.LogInformation("Found {Count} tag associations to remove for note {NoteId}", 
                        noteTagsToRemove.Count, noteId);
                    
                    // Remove all associations
                    _context.NoteTags.RemoveRange(noteTagsToRemove);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully removed {Count} tag associations for note {NoteId}", 
                        noteTagsToRemove.Count, noteId);
                }
                else
                {
                    _logger.LogInformation("No tag associations found for note {NoteId}", noteId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing tag associations for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Deletes notes by IDs using EF Core (soft delete)
        /// REFACTORED: Migrated from stored procedure to EF Core for consistency
        /// </summary>
        public async Task<bool> DeleteNoteAsync(string noteIds)
        {
            try
            {
                _logger.LogInformation("Deleting notes with IDs: {NoteIds} using EF Core", noteIds);

                if (string.IsNullOrWhiteSpace(noteIds))
                {
                    throw new ArgumentException("Note IDs must not be empty", nameof(noteIds));
                }

                // Parse comma-separated note IDs
                var ids = noteIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (!ids.Any())
                {
                    throw new ArgumentException("No valid note IDs provided", nameof(noteIds));
                }

                // Find notes to delete
                var notesToDelete = await _context.Notes
                    .Where(n => ids.Contains(n.NoteId) && n.DeletedAt == null)
                    .ToListAsync();

                if (!notesToDelete.Any())
                {
                    _logger.LogWarning("No notes found to delete with IDs: {NoteIds}", noteIds);
                    return false;
                }

                // Soft delete - set DeletedAt timestamp
                foreach (var note in notesToDelete)
                {
                    note.DeletedAt = DateTime.UtcNow;
                    note.UpdatedAt = DateTime.UtcNow;
                }

                // Also remove tag associations for deleted notes
                var noteTagsToRemove = await _context.NoteTags
                    .Where(nt => ids.Contains(nt.NoteId))
                    .ToListAsync();

                if (noteTagsToRemove.Any())
                {
                    _context.NoteTags.RemoveRange(noteTagsToRemove);
                    _logger.LogInformation("Removed {Count} tag associations for deleted notes", noteTagsToRemove.Count);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully soft deleted {Count} notes with IDs: {NoteIds} using EF Core",
                    notesToDelete.Count, noteIds);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting notes with IDs: {NoteIds}", noteIds);
                throw;
            }
        }
    }
}
