using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for note data access with proper error handling
    /// </summary>
    public class NoteRepository : INoteRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NoteRepository> _logger;

        public NoteRepository(
            ApplicationDbContext context,
            ILogger<NoteRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all notes with comprehensive filtering options
        /// </summary>
        public async Task<List<Note>> GetNotesAsync(NoteFilterOptions filterOptions)
        {
            try
            {
                if (filterOptions == null)
                    throw new ArgumentNullException(nameof(filterOptions));

                _logger.LogInformation(
                    "Getting notes with filters - UserEmail: {UserEmail}, UserId: {UserId}, GetAll: {GetAll}, SearchText: {SearchText}, TagIds: {TagIds}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                    filterOptions.UserEmail, filterOptions.UserId, filterOptions.GetAll, 
                    filterOptions.SearchText, 
                    filterOptions.TagIds != null ? string.Join(",", filterOptions.TagIds) : "null",
                    filterOptions.PageNumber, filterOptions.PageSize);

                var query = _context.Notes
                    .AsNoTracking()
                    .Where(n => n.DeletedAt == null);

                // Filter by user ID if provided
                if (filterOptions.UserId.HasValue)
                {
                    query = query.Where(n => n.UserId == filterOptions.UserId.Value);
                }

                // Filter by search text if provided
                if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                {
                    query = query.Where(n => n.Name.Contains(filterOptions.SearchText) || 
                                           (n.Description != null && n.Description.Contains(filterOptions.SearchText)));
                }

                // TODO: Implement tag filtering when entity_tags relationship is added
                // if (filterOptions.TagIds != null && filterOptions.TagIds.Any())
                // {
                //     query = query.Where(n => n.Tags.Any(t => filterOptions.TagIds.Contains(t.Id)));
                // }

                // Sorting
                query = filterOptions.SortBy?.ToLower() switch
                {
                    "name" => filterOptions.SortOrder?.ToLower() == "asc" 
                        ? query.OrderBy(n => n.Name) 
                        : query.OrderByDescending(n => n.Name),
                    "updatedat" => filterOptions.SortOrder?.ToLower() == "asc" 
                        ? query.OrderBy(n => n.UpdatedAt) 
                        : query.OrderByDescending(n => n.UpdatedAt),
                    _ => filterOptions.SortOrder?.ToLower() == "asc" 
                        ? query.OrderBy(n => n.CreatedAt) 
                        : query.OrderByDescending(n => n.CreatedAt)
                };

                // Pagination
                if (filterOptions.PageNumber.HasValue && filterOptions.PageSize.HasValue)
                {
                    var pageNumber = filterOptions.PageNumber.Value < 1 ? 1 : filterOptions.PageNumber.Value;
                    var pageSize = filterOptions.PageSize.Value < 1 ? 20 : 
                                  filterOptions.PageSize.Value > 100 ? 100 : filterOptions.PageSize.Value;
                    
                    query = query
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize);
                }

                var notes = await query.ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} notes", notes.Count);
                return notes;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting notes with filters");
                throw new InvalidOperationException("Database error while retrieving notes", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes with filters");
                throw;
            }
        }

        /// <summary>
        /// Gets note by ID
        /// </summary>
        public async Task<Note?> GetNoteById(int noteId)
        {
            try
            {
                _logger.LogInformation("Getting note by ID: {NoteId}", noteId);

                var note = await _context.Notes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == noteId && n.DeletedAt == null);

                if (note == null)
                {
                    _logger.LogWarning("Note not found with ID: {NoteId}", noteId);
                }
                else
                {
                    _logger.LogInformation("Successfully retrieved note with ID: {NoteId}", noteId);
                }

                return note;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting note by ID: {NoteId}", noteId);
                throw new InvalidOperationException($"Database error while retrieving note {noteId}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note by ID: {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Creates a new note with optional tags and parent
        /// </summary>
        public async Task<Note> CreateNoteAsync(Note note, List<int>? tagIds, int? parentId)
        {
            try
            {
                if (note == null)
                    throw new ArgumentNullException(nameof(note));

                _logger.LogInformation("Creating note with Name: '{Name}', UserId: {UserId}, TagIds: [{TagIds}], ParentId: {ParentId}",
                    note.Name, note.UserId, tagIds != null ? string.Join(",", tagIds) : "null", parentId);

                // Ensure timestamps
                note.CreatedAt = DateTime.UtcNow;
                note.UpdatedAt = null;
                note.DeletedAt = null;

                _context.Notes.Add(note);
                await _context.SaveChangesAsync();

                // TODO: Add tag associations when entity_tags relationship is implemented
                // if (tagIds != null && tagIds.Any())
                // {
                //     foreach (var tagId in tagIds)
                //     {
                //         _context.EntityTags.Add(new EntityTag
                //         {
                //             EntityType = 3, // note
                //             EntityId = note.Id,
                //             TagId = tagId
                //         });
                //     }
                //     await _context.SaveChangesAsync();
                // }

                _logger.LogInformation("Successfully created note with ID: {NoteId}", note.Id);
                return note;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while creating note");
                throw new InvalidOperationException("Concurrency conflict occurred while creating note", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating note");
                throw new InvalidOperationException("Database error occurred while creating note", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note with Name: '{Name}'", note.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing note with optional tags and parent
        /// </summary>
        public async Task<Note> UpdateNoteAsync(Note note, List<int>? tagIds, int? parentId)
        {
            try
            {
                if (note == null)
                    throw new ArgumentNullException(nameof(note));

                _logger.LogInformation("Updating note ID: {NoteId}, Name: '{Name}', TagIds: [{TagIds}], ParentId: {ParentId}",
                    note.Id, note.Name, tagIds != null ? string.Join(",", tagIds) : "null", parentId);

                var existingNote = await _context.Notes
                    .FirstOrDefaultAsync(n => n.Id == note.Id && n.DeletedAt == null);

                if (existingNote == null)
                {
                    _logger.LogWarning("Note not found for update with ID: {NoteId}", note.Id);
                    throw new KeyNotFoundException($"Note with ID {note.Id} not found");
                }

                // Update properties
                existingNote.Name = note.Name;
                existingNote.Description = note.Description;
                existingNote.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // TODO: Update tag associations when entity_tags relationship is implemented
                // if (tagIds != null)
                // {
                //     // Remove existing tags
                //     var existingTags = _context.EntityTags
                //         .Where(et => et.EntityType == 3 && et.EntityId == note.Id);
                //     _context.EntityTags.RemoveRange(existingTags);
                //     
                //     // Add new tags
                //     foreach (var tagId in tagIds)
                //     {
                //         _context.EntityTags.Add(new EntityTag
                //         {
                //             EntityType = 3,
                //             EntityId = note.Id,
                //             TagId = tagId
                //         });
                //     }
                //     await _context.SaveChangesAsync();
                // }

                _logger.LogInformation("Successfully updated note with ID: {NoteId}", note.Id);
                return existingNote;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while updating note ID: {NoteId}", note.Id);
                throw new InvalidOperationException($"Note {note.Id} was modified by another user", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating note ID: {NoteId}", note.Id);
                throw new InvalidOperationException($"Database error occurred while updating note {note.Id}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note ID: {NoteId}", note.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a note by ID (soft delete)
        /// </summary>
        /// <summary>
        /// Deletes multiple notes by IDs in a single batch operation (soft delete)
        /// </summary>
        public async Task<int> DeleteNotesBatchAsync(List<int> noteIds)
        {
            try
            {
                if (noteIds == null || !noteIds.Any())
                {
                    _logger.LogWarning("Empty note IDs provided for batch deletion");
                    return 0;
                }

                _logger.LogInformation("Batch deleting notes with IDs: {NoteIds}", string.Join(",", noteIds));

                // Update all matching notes in one query
                var affectedRows = await _context.Notes
                    .Where(n => noteIds.Contains(n.Id) && n.DeletedAt == null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(n => n.DeletedAt, DateTime.UtcNow));

                _logger.LogInformation("Successfully batch deleted {Count} notes", affectedRows);
                return affectedRows;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while batch deleting notes with IDs: {NoteIds}", string.Join(",", noteIds));
                throw new InvalidOperationException("Database error occurred while batch deleting notes", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch deleting notes with IDs: {NoteIds}", string.Join(",", noteIds));
                throw;
            }
        }

        /// <summary>
        /// Restores multiple deleted notes by IDs in a single batch operation (undo soft delete)
        /// </summary>
        public async Task<int> UndoDeleteNotesBatchAsync(List<int> noteIds)
        {
            try
            {
                if (noteIds == null || !noteIds.Any())
                {
                    _logger.LogWarning("Empty note IDs provided for batch undo deletion");
                    return 0;
                }

                _logger.LogInformation("Batch restoring notes with IDs: {NoteIds}", string.Join(",", noteIds));

                // Update all matching notes in one query, ignore global filters to find deleted notes
                var affectedRows = await _context.Notes
                    .IgnoreQueryFilters()
                    .Where(n => noteIds.Contains(n.Id) && n.DeletedAt != null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(n => n.DeletedAt, (DateTime?)null)
                        .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));

                _logger.LogInformation("Successfully batch restored {Count} notes", affectedRows);
                return affectedRows;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while batch restoring notes with IDs: {NoteIds}", string.Join(",", noteIds));
                throw new InvalidOperationException("Database error occurred while batch restoring notes", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch restoring notes with IDs: {NoteIds}", string.Join(",", noteIds));
                throw;
            }
        }
    }
}
