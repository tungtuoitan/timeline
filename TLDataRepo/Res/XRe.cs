using System.Data;
using TLMos.Mos;
using DbDataReaderMapper;


namespace TLDataRes.Res
{
    public class XRe

    {
        public async Task<List<Event>> GetEvents()
        {
            try
            {
                List<Event> list = new();

                using (var conn = await OpenedConnection.Create(ApplicationSettings.ERPConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectEvent;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var Event = reader.MapToObject<Event>();
                            list.Add(Event);
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

    }
}
