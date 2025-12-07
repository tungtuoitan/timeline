using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
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
        public async Task<ResultOptions> GetNotesAsync(NoteFilterOptions filterOptions)
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
                    .AsNoTracking();
                    // ✅ Include deleted notes - Frontend will handle display logic

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
                
                return new ResultOptions
                {
                    Success = true,
                    Message = "Notes retrieved successfully",
                    Data = notes.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting notes with filters");
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error while retrieving notes",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes with filters");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Gets note by ID
        /// </summary>
        public async Task<ResultOptions> GetNoteById(int noteId)
        {
            try
            {
                _logger.LogInformation("Getting note by ID: {NoteId}", noteId);

                var note = await _context.Notes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == noteId);
                    // ✅ Include deleted note - Frontend will handle display logic

                if (note == null)
                {
                    _logger.LogWarning("Note not found with ID: {NoteId}", noteId);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"Note not found with ID: {noteId}",
                        Status = 404
                    };
                }
                else
                {
                    _logger.LogInformation("Successfully retrieved note with ID: {NoteId}", noteId);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Note retrieved successfully",
                        Object = note,
                        Status = 200
                    };
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while getting note by ID: {NoteId}", noteId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error while retrieving note",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note by ID: {NoteId}", noteId);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Creates or updates a note with optional tags and parent (upsert)
        /// </summary>
        public async Task<ResultOptions> UpsertNoteAsync(Note note, List<int>? tagIds, int? parentId)
        {
            try
            {
                if (note == null)
                    throw new ArgumentNullException(nameof(note));

                bool isUpdate = note.Id > 0;

                if (isUpdate)
                {
                    // UPDATE existing note
                    _logger.LogInformation("Updating note ID: {NoteId}, Name: '{Name}', UserId: {UserId}, TagIds: [{TagIds}], ParentId: {ParentId}",
                        note.Id, note.Name, note.UserId, tagIds != null ? string.Join(",", tagIds) : "null", parentId);

                    var existingNote = await _context.Notes
                        .FirstOrDefaultAsync(n => n.Id == note.Id && n.DeletedAt == null);

                    if (existingNote == null)
                    {
                        _logger.LogWarning("Note not found for update with ID: {NoteId}", note.Id);
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"Note with ID {note.Id} not found",
                            Status = 404
                        };
                    }

                    // Validate UserId exists before update
                    var userExists = await _context.Users.AnyAsync(u => u.Id == note.UserId);
                    if (!userExists)
                    {
                        _logger.LogError("User not found with ID: {UserId}", note.UserId);
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"User with ID {note.UserId} not found",
                            Status = 400
                        };
                    }

                    // Update properties
                    existingNote.Name = note.Name;
                    existingNote.Description = note.Description;
                    existingNote.UserId = note.UserId;
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
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Note updated successfully",
                        Object = existingNote,
                        Status = 200
                    };
                }
                else
                {
                    // CREATE new note
                    _logger.LogInformation("Creating note with Name: '{Name}', UserId: {UserId}, TagIds: [{TagIds}], ParentId: {ParentId}",
                        note.Name, note.UserId, tagIds != null ? string.Join(",", tagIds) : "null", parentId);

                    // Validate UserId exists before create
                    var userExists = await _context.Users.AnyAsync(u => u.Id == note.UserId);
                    if (!userExists)
                    {
                        _logger.LogError("User not found with ID: {UserId}", note.UserId);
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"User with ID {note.UserId} not found",
                            Status = 400
                        };
                    }

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
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Note created successfully",
                        Object = note,
                        Status = 201
                    };
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while upserting note ID: {NoteId}", note.Id);
                return new ResultOptions
                {
                    Success = false,
                    Message = note.Id > 0 
                        ? $"Note {note.Id} was modified by another user" 
                        : "Concurrency conflict occurred while creating note",
                    Status = 409
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while upserting note ID: {NoteId}, UserId: {UserId}", 
                    note.Id, note.UserId);
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while saving note",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting note ID: {NoteId}, Name: '{Name}'", 
                    note.Id, note.Name);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Deletes multiple notes by IDs in a single batch operation (soft or hard delete)
        /// </summary>
        public async Task<ResultOptions> DeleteNotesBatchAsync(List<int> noteIds, bool isHardDelete = false)
        {
            try
            {
                if (noteIds == null || !noteIds.Any())
                {
                    _logger.LogWarning("Empty note IDs provided for batch deletion");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No note IDs provided",
                        Status = 400
                    };
                }

                _logger.LogInformation("Batch deleting notes with IDs: {NoteIds} (HardDelete: {IsHardDelete})", 
                    string.Join(",", noteIds), isHardDelete);

                int affectedRows;

                if (isHardDelete)
                {
                    // Hard delete - permanently remove from database
                    affectedRows = await _context.Notes
                        .Where(n => noteIds.Contains(n.Id))
                        .ExecuteDeleteAsync();
                }
                else
                {
                    // Soft delete - set deleted_at timestamp
                    affectedRows = await _context.Notes
                        .Where(n => noteIds.Contains(n.Id) && n.DeletedAt == null)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(n => n.DeletedAt, DateTime.UtcNow));
                }

                if (affectedRows == 0)
                {
                    _logger.LogWarning("No notes found to delete with IDs: {NoteIds}", string.Join(",", noteIds));
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"No notes found with IDs: {string.Join(",", noteIds)}",
                        Status = 404
                    };
                }

                _logger.LogInformation("Successfully batch deleted {Count} notes (HardDelete: {IsHardDelete})", 
                    affectedRows, isHardDelete);
                    
                return new ResultOptions
                {
                    Success = true,
                    Message = $"Successfully deleted {affectedRows} note(s)",
                    Status = 200
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while batch deleting notes with IDs: {NoteIds}", string.Join(",", noteIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while batch deleting notes",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch deleting notes with IDs: {NoteIds}", string.Join(",", noteIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Restores multiple deleted notes by IDs in a single batch operation (undo soft delete)
        /// </summary>
        public async Task<ResultOptions> UndoDeleteNotesBatchAsync(List<int> noteIds)
        {
            try
            {
                if (noteIds == null || !noteIds.Any())
                {
                    _logger.LogWarning("Empty note IDs provided for batch undo deletion");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No note IDs provided",
                        Status = 400
                    };
                }

                _logger.LogInformation("Batch restoring notes with IDs: {NoteIds}", string.Join(",", noteIds));

                // Update all matching notes in one query, ignore global filters to find deleted notes
                var affectedRows = await _context.Notes
                    .IgnoreQueryFilters()
                    .Where(n => noteIds.Contains(n.Id) && n.DeletedAt != null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(n => n.DeletedAt, (DateTime?)null)
                        .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));

                if (affectedRows == 0)
                {
                    _logger.LogWarning("No deleted notes found with IDs: {NoteIds}", string.Join(",", noteIds));
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"No deleted notes found with IDs: {string.Join(",", noteIds)}",
                        Status = 404
                    };
                }

                _logger.LogInformation("Successfully batch restored {Count} notes", affectedRows);
                return new ResultOptions
                {
                    Success = true,
                    Message = $"Successfully restored {affectedRows} note(s)",
                    Status = 200
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while batch restoring notes with IDs: {NoteIds}", string.Join(",", noteIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = "Database error occurred while batch restoring notes",
                    Status = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch restoring notes with IDs: {NoteIds}", string.Join(",", noteIds));
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }
    }
}
