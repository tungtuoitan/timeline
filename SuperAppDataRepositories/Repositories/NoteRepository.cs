using System.Data;
using SuperAppModels.Mos;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Extensions;
using SuperAppDataRepositories.Data;
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
    }
}
