using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Extensions;
using SuperAppDataRepositories.Data;
using SuperAppModels.Models;
using SuperAppModels.DTOs;

namespace SuperAppDataRepositories.Repositories
{
    public class NoteRepository : BaseRepository, INoteRepository
    {
        public NoteRepository(ILogger<NoteRepository> logger, IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
        }

        public async Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null)
        {
            return await ExecuteStoredProcedureAsync(
                StoredProcedures.spSelectNotes,
                addParameters: async (command) =>
                {
                    command.Parameters.Add(new SqlParameter("@iv_getAll", getAll));
                    AddParameterIfNotNull(command, "@iv_SearchText", searchText);
                    AddParameterIfNotNull(command, "@iv_Types", types);
                    AddParameterIfNotNull(command, "@iv_Tags", tags);
                    AddParameterIfNotNull(command, "@iv_CreatedBy", createdBy);
                    await Task.CompletedTask;
                },
                mapResult: MapToListAsync<Note>,
                useSuperAppConnection: true
            );
        }

        public async Task<NotesResult> IuNote(Note note)
        {
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
            ResultOptions options;

            if (!HasError(errorParam, out string errorMessage))
            {
                options = new ResultOptions
                {
                    Message = note.NoteId == 0 ? "Successfully saved record." : "Successfully updated record",
                    Success = true,
                    Reference = notes.ToString(),
                };
            }
            else
            {
                options = new ResultOptions
                {
                    Message = "Error saving record",
                    Success = false,
                };
            }

            return new NotesResult { Notes = notes, Options = options };
        }

        public async Task<bool> DNote(int noteId)
        {
            try
            {
                var (result, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spDeleteNote,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) => new List<object>(),
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                return !HasError(errorParam, out string errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting note with ID: {NoteId}", noteId);
                return false;
            }
        }

        public async Task<Note?> GetNoteById(int noteId)
        {
            try
            {
                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectNoteById,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToSingleAsync<Note>,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting note with ID: {NoteId}", noteId);
                throw;
            }
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            try
            {
                _logger.LogInformation("Creating new note with title: {Title}", note.NoteName);

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

                _logger.LogInformation("Successfully created note with ID: {NoteId}", createdNote.NoteId);
                return createdNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating note");
                throw;
            }
        }

        public async Task<Note> UpdateNoteAsync(Note note)
        {
            try
            {
                _logger.LogInformation("Updating note with ID: {NoteId}", note.NoteId);

                if (note.NoteId <= 0)
                {
                    throw new ArgumentException("Note ID must be greater than 0 for updates", nameof(note));
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

                _logger.LogInformation("Successfully updated note with ID: {NoteId}", updatedNote.NoteId);
                return updatedNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating note with ID: {NoteId}", note.NoteId);
                throw;
            }
        }

        public async Task<bool> DeleteNoteAsync(int noteId)
        {
            try
            {
                _logger.LogInformation("Deleting note with ID: {NoteId}", noteId);

                if (noteId <= 0)
                {
                    throw new ArgumentException("Note ID must be greater than 0", nameof(noteId));
                }

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
                    StoredProcedures.spDeleteNote,
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
                        return (object?)null;
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error deleting note: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to delete note: {errorMessage}");
                }

                _logger.LogInformation("Successfully deleted note with ID: {NoteId}", noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting note with ID: {NoteId}", noteId);
                throw;
            }
        }
    }
}
