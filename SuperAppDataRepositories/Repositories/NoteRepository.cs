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
                                  && tagIds.Contains(wi.ParentTagId)
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







        public async Task<Note?> GetNoteById(int noteId)
        {
            try
            {
                var notes = await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectNoteById,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        await Task.CompletedTask;
                    },
                    mapResult: MapNotesWithTagsAsync,
                    useSuperAppConnection: true
                );

                return notes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting note with ID: {NoteId}", noteId);
                throw;
            }
        }

        public async Task<Note> CreateNoteAsync(Note note, List<int>? tagIds = null, int? createdByUserId = null)
        {
            try
            {
                _logger.LogInformation("Creating new note with title: {Title}", note.Name);

                // Set the UserId if provided (replacing CreatedBy concept)
                if (createdByUserId.HasValue)
                {
                    note.UserId = createdByUserId.Value;
                }

                var (notes, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertUpdateNote,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        // Set NoteId to 0 for new notes
                        note.NoteId = 0;
                        DataTable noteTable = note.ToDataTable();
                        AddStructuredParameter(command, "@Note", noteTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToListAsync<Note>,
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error creating note: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to create note: {errorMessage}");
                }

                var createdNote = notes.FirstOrDefault();
                if (createdNote == null)
                {
                    throw new InvalidOperationException("Failed to create note: No note returned from database");
                }

                // Associate tags with the note if provided
                if (tagIds != null && tagIds.Any())
                {
                    await AssociateTagsWithNoteAsync(createdNote.NoteId, tagIds, note.UserId);
                    
                    // TODO: Reload the note to get updated tags from the database once Tag relationship is defined
                }

                _logger.LogInformation("Successfully created note with ID: {NoteId}", 
                    createdNote.NoteId);
                return createdNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating note");
                throw;
            }
        }

        private async Task AssociateTagsWithNoteAsync(int noteId, List<int> tagIds, int createdBy)
        {
            // Filter out invalid tag IDs before processing
            var validTagIds = tagIds?.Where(id => id > 0).ToList();
            
            if (validTagIds == null || !validTagIds.Any())
            {
                _logger.LogInformation("No valid tag IDs to associate with note {NoteId}", noteId);
                return;
            }

            _logger.LogInformation("Associating {TagCount} valid tags [{TagIds}] with note {NoteId}", 
                validTagIds.Count, string.Join(",", validTagIds), noteId);

            foreach (var tagId in validTagIds)
            {
                try
                {
                    await ExecuteStoredProcedureAsync(
                        StoredProcedures.spInsertTaggable,
                        addParameters: async (command) =>
                        {
                            command.Parameters.Add(new SqlParameter("@iv_TaggableId", noteId));
                            command.Parameters.Add(new SqlParameter("@iv_TaggableType", "Note"));
                            command.Parameters.Add(new SqlParameter("@iv_TagId", tagId));
                            AddParameterIfNotNull(command, "@iv_CreatedBy", createdBy);
                            await Task.CompletedTask;
                        },
                        mapResult: async (reader) =>
                        {
                            await Task.CompletedTask;
                            return new List<object>();
                        },
                        useSuperAppConnection: true
                    );

                    _logger.LogDebug("Successfully associated tag {TagId} with note {NoteId}", tagId, noteId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to associate tag {TagId} with note {NoteId}", tagId, noteId);
                    // Continue with other tags even if one fails
                }
            }
        }

        public async Task<Note> UpdateNoteAsync(Note note, List<int>? tagIds = null, int? createdByUserId = null)
        {
            try
            {
                _logger.LogInformation("Updating note with ID: {NoteId}", note.NoteId);

                if (note.NoteId <= 0)
                {
                    throw new ArgumentException("Note ID must be greater than 0 for updates", nameof(note));
                }

                // Check if note exists first
                var existingNote = await GetNoteById(note.NoteId);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note with ID {NoteId} not found for update", note.NoteId);
                    throw new InvalidOperationException($"Note with ID {note.NoteId} not found");
                }

                // Set the UserId if provided (replacing CreatedBy concept)
                if (createdByUserId.HasValue)
                {
                    note.UserId = createdByUserId.Value;
                }

                var (notes, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertUpdateNote,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        DataTable noteTable = note.ToDataTable();
                        AddStructuredParameter(command, "@Note", noteTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToListAsync<Note>,
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error updating note: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to update note: {errorMessage}");
                }

                var updatedNote = notes.FirstOrDefault();
                if (updatedNote == null)
                {
                    throw new InvalidOperationException("Failed to update note: No note returned from database");
                }

                // Update tag associations if provided
                if (tagIds != null)
                {
                    // Remove all existing tag associations
                    await RemoveAllTagAssociationsAsync(updatedNote.NoteId);
                    
                    // Add new tag associations
                    if (tagIds.Any())
                    {
                        await AssociateTagsWithNoteAsync(updatedNote.NoteId, tagIds, note.UserId);
                    }
                    
                    // Reload the note to get updated tags from the database
                    // Note: Tags navigation property handling removed until EF configuration is set up
                }

                _logger.LogInformation("Successfully updated note with ID: {NoteId}", 
                    updatedNote.NoteId);
                return updatedNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating note with ID: {NoteId}", note.NoteId);
                throw;
            }
        }

        private async Task RemoveAllTagAssociationsAsync(int noteId)
        {
            try
            {
                await ExecuteStoredProcedureAsync(
                    StoredProcedures.spDeleteTaggablesByEntity,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_TaggableId", noteId));
                        command.Parameters.Add(new SqlParameter("@iv_TaggableType", "Note"));
                        await Task.CompletedTask;
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove tag associations for note {NoteId}", noteId);
                throw;
            }
        }

        public async Task<bool> DeleteNoteAsync(string noteIds)
        {
            try
            {
                _logger.LogInformation("Deleting notes with IDs: {NoteIds}", noteIds);

                if (string.IsNullOrWhiteSpace(noteIds))
                {
                    throw new ArgumentException("Note IDs must not be empty", nameof(noteIds));
                }

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spDeleteNote,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteIds", noteIds));
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
                    _logger.LogError("Error deleting notes: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to delete notes: {errorMessage}");
                }

                _logger.LogInformation("Successfully deleted notes with IDs: {NoteIds}", noteIds);
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
