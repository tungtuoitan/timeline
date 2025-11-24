using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Repositories;

/// <summary>
/// Diagnostic tool to test the Tags stored procedure and database connection
/// </summary>
public class TagsDiagnostic
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // You'll need to configure your connection string here
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<TagsDiagnostic>()
            .Build();
            
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<IConnectionFactory, ConnectionFactory>();
        services.AddScoped<TagRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<TagsDiagnostic>>();
        var connectionFactory = serviceProvider.GetRequiredService<IConnectionFactory>();
        
        try
        {
            logger.LogInformation("Starting Tags diagnostic...");
            
            // Test 1: Check database connection
            logger.LogInformation("Testing database connection...");
            using (var conn = await connectionFactory.CreateSuperAppConnectionAsync())
            {
                logger.LogInformation("Database connection successful. Server: {Server}, Database: {Database}", 
                    conn.DataSource, conn.Database);
            }
            
            // Test 2: Check if stored procedure exists
            logger.LogInformation("Checking if stored procedure [dbo].[usp_s_tags] exists...");
            using (var conn = await connectionFactory.CreateSuperAppConnectionAsync())
            {
                using var command = conn.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM sys.procedures 
                    WHERE name = 'usp_s_tags' AND schema_id = SCHEMA_ID('dbo')";
                    
                var count = (int)await command.ExecuteScalarAsync();
                logger.LogInformation("Stored procedure exists: {Exists}", count > 0);
                
                if (count == 0)
                {
                    logger.LogError("Stored procedure [dbo].[usp_s_tags] does not exist!");
                    return;
                }
            }
            
            // Test 3: Check stored procedure parameters
            logger.LogInformation("Checking stored procedure parameters...");
            using (var conn = await connectionFactory.CreateSuperAppConnectionAsync())
            {
                using var command = conn.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        p.parameter_id,
                        p.name,
                        TYPE_NAME(p.user_type_id) as type_name,
                        p.max_length,
                        p.is_output
                    FROM sys.parameters p
                    INNER JOIN sys.procedures pr ON p.object_id = pr.object_id
                    WHERE pr.name = 'usp_s_tags' AND pr.schema_id = SCHEMA_ID('dbo')
                    ORDER BY p.parameter_id";
                    
                using var reader = await command.ExecuteReaderAsync();
                logger.LogInformation("Stored procedure parameters:");
                
                while (await reader.ReadAsync())
                {
                    logger.LogInformation("  Parameter: {Name}, Type: {Type}, Length: {Length}, IsOutput: {IsOutput}",
                        reader["name"], reader["type_name"], reader["max_length"], reader["is_output"]);
                }
            }
            
            // Test 4: Check actual columns returned by stored procedure
            logger.LogInformation("Checking columns returned by stored procedure...");
            using (var conn = await connectionFactory.CreateSuperAppConnectionAsync())
            {
                using var command = conn.CreateCommand();
                command.CommandText = "[dbo].[usp_s_tags]";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@user_id", 1));
                
                using var reader = await command.ExecuteReaderAsync();
                
                // Log schema information
                var schemaTable = reader.GetSchemaTable();
                if (schemaTable != null)
                {
                    logger.LogInformation("Stored procedure columns:");
                    foreach (DataRow row in schemaTable.Rows)
                    {
                        var columnName = row["ColumnName"]?.ToString();
                        var dataType = row["DataType"]?.ToString();
                        var allowDBNull = row["AllowDBNull"]?.ToString();
                        logger.LogInformation("  Column: {ColumnName}, Type: {DataType}, AllowNull: {AllowNull}",
                            columnName, dataType, allowDBNull);
                    }
                }
                
                // Log first few rows to see actual data
                logger.LogInformation("Sample data from stored procedure:");
                int rowCount = 0;
                while (await reader.ReadAsync() && rowCount < 3)
                {
                    var values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    
                    var fieldData = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        fieldData.Add($"{reader.GetName(i)}={values[i] ?? "NULL"}");
                    }
                    
                    logger.LogInformation("  Row {RowNumber}: {FieldData}", rowCount + 1, string.Join(", ", fieldData));
                    rowCount++;
                }
                
                logger.LogInformation("Total rows in result set: Will be counted...");
            }
            
            // Test 5: Test manual mapping to see if it's a DbDataReaderMapper issue
            logger.LogInformation("Testing manual mapping from SqlDataReader...");
            using (var conn = await connectionFactory.CreateSuperAppConnectionAsync())
            {
                using var command = conn.CreateCommand();
                command.CommandText = "[dbo].[usp_s_tags]";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@user_id", 1));
                
                using var reader = await command.ExecuteReaderAsync();
                
                var manualTags = new List<object>();
                int manualRowCount = 0;
                
                while (await reader.ReadAsync() && manualRowCount < 3)
                {
                    try
                    {
                        // Try to manually map to see what columns are available - test both cases
                        var tag = new
                        {
                            // Test lowercase (from stored procedure)
                            Id = TryGetInt32(reader, "id") ?? TryGetInt32(reader, "Id") ?? 0,
                            Name = TryGetString(reader, "name") ?? TryGetString(reader, "Name") ?? "",
                            UserId = TryGetInt32(reader, "user_id") ?? TryGetInt32(reader, "UserId") ?? 0,
                            ParentId = TryGetNullableInt32(reader, "parent_id") ?? TryGetNullableInt32(reader, "ParentId"),
                            Path = TryGetString(reader, "path") ?? TryGetString(reader, "Path"),
                            Color = TryGetString(reader, "color") ?? TryGetString(reader, "Color"),
                            CreatedAt = TryGetDateTime(reader, "created_at") ?? TryGetDateTime(reader, "CreatedAt"),
                            Depth = TryGetNullableInt32(reader, "depth") ?? TryGetNullableInt32(reader, "Depth"),
                        };
                        
                        logger.LogInformation("  Manual mapping success - Tag: ID={Id}, Name={Name}, UserId={UserId}, ParentId={ParentId}",
                            tag.Id, tag.Name, tag.UserId, tag.ParentId);
                        manualTags.Add(tag);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in manual mapping for row {RowNumber}", manualRowCount + 1);
                    }
                    manualRowCount++;
                }
                
                logger.LogInformation("Manual mapping created {Count} objects", manualTags.Count);
            }
            
            // Test 6: Test with TagRepository (this might fail due to mapping issues)
            logger.LogInformation("Testing TagRepository.GetTags with userId = 1...");
            try
            {
                var tagRepository = serviceProvider.GetRequiredService<TagRepository>();
                var tags = await tagRepository.GetTags(1);
                
                logger.LogInformation("Retrieved {TagCount} tags for userId = 1", tags.Count);
                foreach (var tag in tags.Take(3)) // Show only first 3
                {
                    logger.LogInformation("  Tag: ID={Id}, Name={Name}, UserId={UserId}", 
                        tag.Id, tag.Name, tag.UserId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in TagRepository.GetTags - this indicates a mapping issue");
            }
        
        // Test 7: Check if there are any tags in the database at all
        logger.LogInformation("Checking total tag count in database...");
        using (var conn = await connectionFactory.CreateSuperAppConnectionAsync())
        {
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM tags";
                
            try
            {
                var totalTags = (int)await command.ExecuteScalarAsync();
                logger.LogInformation("Total tags in database: {TotalTags}", totalTags);
                
                if (totalTags > 0)
                {
                    command.CommandText = "SELECT DISTINCT user_id FROM tags ORDER BY user_id";
                    using var reader = await command.ExecuteReaderAsync();
                    logger.LogInformation("Available UserIds in tags table:");
                    while (await reader.ReadAsync())
                    {
                        logger.LogInformation("  UserId: {UserId}", reader["user_id"]);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not query tags table directly. Table might not exist or have different structure.");
            }
        }
        
        logger.LogInformation("Tags diagnostic completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tags diagnostic failed");
        }
    }
    
    // Helper methods for safe column reading
    static int? TryGetNullableInt32(SqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }
        catch
        {
            return null;
        }
    }
    
    static int TryGetInt32(SqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }
        catch
        {
            return 0;
        }
    }
    
    static string? TryGetString(SqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch
        {
            return null;
        }
    }
    
    static DateTime? TryGetDateTime(SqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
        catch
        {
            return null;
        }
    }
}