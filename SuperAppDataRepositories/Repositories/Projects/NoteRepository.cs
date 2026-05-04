using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppModels.Helpers;
using SuperAppModels.Utils;

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
                    "Getting notes with filters - UserEmail: {UserEmail}, UserId: {UserId}, SearchText: {SearchText}, TagIds: {TagIds}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                    filterOptions.UserEmail, filterOptions.UserId, 
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

                // ===== NEW FILTERS =====

                if (filterOptions.Ids?.Count > 0)
                {
                    query = query.Where(n => filterOptions.Ids.Contains(n.Id));
                }

                // Filter by workspace item IDs - find notes through workspace_items
                if (filterOptions.WorkspaceItemIds?.Count > 0)
                {
                    // Get entityIds from workspace_items where id IN workspaceItemIds AND entityType = 3 (note)
                    var noteIds = await _context.WorkspaceItems
                        .AsNoTracking()
                        .Where(wi => filterOptions.WorkspaceItemIds.Contains(wi.Id) && wi.EntityType == 3)
                        .Select(wi => wi.EntityId)
                        .ToListAsync();

                    if (noteIds.Any())
                    {
                        query = query.Where(n => noteIds.Contains(n.Id));
                    }
                    else
                    {
                        // No matching notes found, return empty result
                        query = query.Where(n => false);
                    }
                }

                // Filter by status code (comma-separated list)
                if (filterOptions.StatusCodes != null && filterOptions.StatusCodes.Any())
                {
                    query = query.Where(n => n.StatusCode != null && filterOptions.StatusCodes.Contains(n.StatusCode));
                }

                // Filter by deletedAt
                if (!string.IsNullOrEmpty(filterOptions.DeletedAt))
                {
                    if (filterOptions.DeletedAt == "null")
                    {
                        // Show only active (not deleted)
                        query = query.Where(n => n.DeletedAt == null);
                    }
                    else if (filterOptions.DeletedAt == "notNull")
                    {
                        // Show only deleted
                        query = query.Where(n => n.DeletedAt != null);
                    }
                }

                // Filter by created date range
                if (filterOptions.CreatedFrom.HasValue)
                {
                    query = query.Where(n => n.CreatedAt >= filterOptions.CreatedFrom.Value);
                }

                if (filterOptions.CreatedTo.HasValue)
                {
                    query = query.Where(n => n.CreatedAt <= filterOptions.CreatedTo.Value);
                }


                // ===== END NEW FILTERS =====

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
        /// Permanently deletes multiple notes by IDs with cascade to workspace_items
        /// All-or-nothing: Either all deletes succeed, or transaction is rolled back
        /// </summary>
        public async Task<ResultOptions> DeleteNotesAsync(List<int> noteIds)
        {
            // Create execution strategy to handle retry logic with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ===== STEP 1: Validate Request =====
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

                    _logger.LogInformation("Starting batch delete transaction for notes with IDs: {NoteIds}",
                        string.Join(",", noteIds));

                    // ===== STEP 2: Delete Workspace Items =====
                    var workspaceItemsDeleted = await _context.WorkspaceItems
                        .Where(wi => wi.EntityType == 3 && noteIds.Contains(wi.EntityId))
                        .ExecuteDeleteAsync();

                    _logger.LogInformation("Deleted {Count} workspace_items for notes in transaction", workspaceItemsDeleted);

                    // ===== STEP 3: Delete Notes =====
                    var notesDeleted = await _context.Notes
                        .Where(n => noteIds.Contains(n.Id))
                        .ExecuteDeleteAsync();

                    if (notesDeleted == 0)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning("No notes found to delete with IDs: {NoteIds} - transaction rolled back",
                            string.Join(",", noteIds));
                        return new ResultOptions
                        {
                            Success = false,
                            Message = $"No notes found with IDs: {string.Join(",", noteIds)} - transaction rolled back",
                            Status = 404
                        };
                    }

                    // ===== STEP 4: Commit Transaction =====
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully committed batch delete transaction: {Count} note(s) and {WICount} workspace_items deleted",
                        notesDeleted, workspaceItemsDeleted);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully permanently deleted {notesDeleted} note(s) and {workspaceItemsDeleted} workspace_items",
                        Status = 200
                    };
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database error while deleting notes with IDs: {NoteIds} - transaction rolled back",
                        string.Join(",", noteIds));
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Database error occurred while deleting notes - all changes rolled back",
                        Status = 500
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting notes with IDs: {NoteIds} - transaction rolled back",
                        string.Join(",", noteIds));
                    return new ResultOptions
                    {
                        Success = false,
                        Message = ex.Message + " - all changes rolled back",
                        Status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Batch upserts multiple notes in a single transaction (all-or-nothing)
        /// Either all notes are upserted successfully, or the entire operation is rolled back
        /// </summary>
        public async Task<ResultOptions> UpsertNotesAsync(List<(Note note, List<int>? tagIds)> noteRequests)
        {
            // Create execution strategy to handle retry logic with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ===== STEP 1: Validate Batch Request =====
                    if (noteRequests == null || !noteRequests.Any())
                    {
                        _logger.LogWarning("Empty note batch upsert request");
                        return new ResultOptions
                        {
                            Success = false,
                            Message = "No notes provided for batch upsert",
                            Status = 400
                        };
                    }

                    _logger.LogInformation("Starting batch upsert transaction for {Count} notes", noteRequests.Count);

                    var errors = new List<string>();

                    // Validate all notes are not null
                    for (int i = 0; i < noteRequests.Count; i++)
                    {
                        if (noteRequests[i].note == null)
                        {
                            errors.Add($"Note at index {i} is null");
                        }
                    }

                    if (errors.Any())
                    {
                        await transaction.RollbackAsync();
                        return ResultOptions.Fail(errors, 400);
                    }

                    // ===== STEP 2: Preload Data =====
                    _logger.LogInformation("Preloading users and existing notes");

                    // Get all unique user IDs
                    var userIds = noteRequests.Select(r => r.note.UserId).Distinct().ToList();

                    // Load all users in one query
                    var existingUsers = await _context.Users
                        .Where(u => userIds.Contains(u.Id))
                        .Select(u => u.Id)
                        .ToListAsync();

                    // Validate all users exist
                    var missingUserIds = userIds.Except(existingUsers).ToList();
                    if (missingUserIds.Any())
                    {
                        errors.Add($"Users not found with IDs: {string.Join(", ", missingUserIds)}");
                    }

                    // Get all note IDs that need to be updated (ID > 0)
                    var noteIdsToUpdate = noteRequests
                        .Where(r => r.note.Id > 0)
                        .Select(r => r.note.Id)
                        .ToList();

                    // Load all existing notes in one query (including soft-deleted for restore)
                    var existingNotesDict = await _context.Notes
                        .IgnoreQueryFilters()
                        .Where(n => noteIdsToUpdate.Contains(n.Id))
                        .ToDictionaryAsync(n => n.Id, n => n);

                    // Validate all notes to update exist
                    var missingNoteIds = noteIdsToUpdate.Except(existingNotesDict.Keys).ToList();
                    if (missingNoteIds.Any())
                    {
                        errors.Add($"Notes not found with IDs: {string.Join(", ", missingNoteIds)}");
                    }

                    if (errors.Any())
                    {
                        await transaction.RollbackAsync();
                        return ResultOptions.Fail(errors, 400);
                    }

                    // ===== STEP 3: Upsert Notes =====
                    _logger.LogInformation("Upserting {Count} notes", noteRequests.Count);

                    var upsertedNotes = new List<Note>();

                    foreach (var (note, tagIds) in noteRequests)
                    {
                        bool isUpdate = note.Id > 0;

                        if (isUpdate)
                        {
                            // UPDATE existing note
                            var existingNote = existingNotesDict[note.Id];

                            var operation = note.DeletedAt.HasValue ? "Soft deleting" :
                                           (existingNote.DeletedAt.HasValue ? "Restoring" : "Updating");

                            _logger.LogInformation("{Operation} note ID: {NoteId}, Name: '{Name}', UserId: {UserId}",
                                operation, note.Id, note.Name, note.UserId);

                            // Update properties
                            existingNote.Name = note.Name;
                            existingNote.Description = note.Description;
                            existingNote.UserId = note.UserId;
                            existingNote.StatusCode = note.StatusCode;
                            existingNote.Icon = note.Icon;
                            existingNote.Color = note.Color;
                            existingNote.DeletedAt = note.DeletedAt;  // Handle soft delete/restore
                            existingNote.UpdatedAt = VietnamDateTime.Now();

                            upsertedNotes.Add(existingNote);
                        }
                        else
                        {
                            // CREATE new note
                            _logger.LogInformation("Creating note with Name: '{Name}', UserId: {UserId}",
                                note.Name, note.UserId);

                            // Ensure timestamps
                            note.CreatedAt = VietnamDateTime.Now();
                            note.UpdatedAt = null;
                            note.DeletedAt = null;

                            _context.Notes.Add(note);
                            upsertedNotes.Add(note);
                        }
                    }

                    // Save all changes in the transaction (first save to get IDs)
                    await _context.SaveChangesAsync();

                    // ===== STEP 4: Replace Negative NoteIds in Wiki Links =====
                    // For newly created notes, replace negative noteIds in description with real noteId
                    var notesToUpdateDescription = new List<Note>();
                    
                    foreach (var note in upsertedNotes)
                    {
                        // Only process newly created notes with description containing wiki links
                        if (NoteDescriptionProcessor.ContainsWikiLinks(note.Description))
                        {
                            var updatedDescription = NoteDescriptionProcessor.ReplaceNegativeNoteIdInWikiLinks(note.Description, note.Id);

                            // Only update if description changed
                            if (updatedDescription != note.Description)
                            {
                                _logger.LogInformation(
                                    "Replacing negative noteIds in description for note ID: {NoteId}",
                                    note.Id);

                                note.Description = updatedDescription;
                                note.UpdatedAt = VietnamDateTime.Now();
                                notesToUpdateDescription.Add(note);
                            }
                        }
                    }

                    // Save again if any descriptions were updated
                    if (notesToUpdateDescription.Any())
                    {
                        _logger.LogInformation(
                            "Updating {Count} note descriptions with real noteIds",
                            notesToUpdateDescription.Count);
                        await _context.SaveChangesAsync();
                    }

                    // Commit transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully committed batch upsert transaction for {Count} notes",
                        noteRequests.Count);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully upserted {noteRequests.Count} notes in batch",
                        Data = upsertedNotes.Cast<object>().ToList(),
                        Status = 200
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // ===== STEP 4: Handle Exceptions =====
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency conflict during batch upsert - transaction rolled back");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Concurrency conflict occurred - all changes rolled back",
                        Status = 409
                    };
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database error during batch upsert - transaction rolled back");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Database error occurred - all changes rolled back",
                        Status = 500
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during batch upsert - transaction rolled back");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = ex.Message + " - all changes rolled back",
                        Status = 500
                    };
                }
            });
        }
    }
}
