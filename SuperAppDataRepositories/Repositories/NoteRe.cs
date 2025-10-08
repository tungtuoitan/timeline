using System.Data;
using SuperAppModels.Mos;
using DbDataReaderMapper;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Extensions;
using SuperAppModels.DTOs;

namespace SuperAppDataRepositories.Repositories
{
    public class NoteRe : INoteRe
    {
        private readonly ILogger<NoteRe> _logger;
        
        public NoteRe(ILogger<NoteRe> logger)
        {
            _logger = logger;
        }
        
        public async Task<List<Note>> GetNotes(bool getAll = false, string? searchText = null, string? types = null, string? tags = null, string? createdBy = null)
        {
            try
            {
                List<Note> list = new();
                using (var conn = await OpenedConnection.Create(ApplicationSettings.SuperAppConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectNotes;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    // Add parameters for the stored procedure
                    command.Parameters.Add(new SqlParameter("@iv_getAll", getAll));
                    
                    if (!string.IsNullOrEmpty(searchText))
                        command.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));
                    else
                        command.Parameters.Add(new SqlParameter("@iv_SearchText", DBNull.Value));
                        
                    if (!string.IsNullOrEmpty(types))
                        command.Parameters.Add(new SqlParameter("@iv_Types", types));
                    else
                        command.Parameters.Add(new SqlParameter("@iv_Types", DBNull.Value));
                        
                    if (!string.IsNullOrEmpty(tags))
                        command.Parameters.Add(new SqlParameter("@iv_Tags", tags));
                    else
                        command.Parameters.Add(new SqlParameter("@iv_Tags", DBNull.Value));
                        
                    if (!string.IsNullOrEmpty(createdBy))
                        command.Parameters.Add(new SqlParameter("@iv_CreatedBy", createdBy));
                    else
                        command.Parameters.Add(new SqlParameter("@iv_CreatedBy", DBNull.Value));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var note = reader.MapToObject<Note>();
                            list.Add(note);
                        }
                    }
                }
                return list;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<NotesResult> IuNote(Note note)
        {
            try
            {
                List<Note> notes = new List<Note>();
                ResultOptions options;
                using (var conn = await OpenedConnection.Create(ApplicationSettings.SuperAppConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spInsertUpdateNote;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    command.Parameters.Clear();

                    DataTable Note = note.ToDataTable();

                    command.Parameters.Add(new SqlParameter("@Note", SqlDbType.Structured)
                    {
                        Value = Note
                    });

                    SqlParameter errorMsg = new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(errorMsg);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = reader.MapToObject<Note>();
                            notes.Add(item);
                        }
                    }

                    string msg = errorMsg.Value.ToString() ?? "";

                    if (string.IsNullOrEmpty(msg))
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
            }
            catch (Exception ex)
            {
                // Log l?i n?u c?n thi?t tr??c khi ném l?i
                throw;
            }
        }
    }
}