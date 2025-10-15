CREATE OR ALTER PROCEDURE usp_rebuild_fragmented_indexes
    @fragmentation_threshold INT = 30  -- Rebuild if fragmentation > 30%
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @table_name NVARCHAR(255);
    DECLARE @index_name NVARCHAR(255);
    DECLARE @fragmentation FLOAT;
    
    DECLARE index_cursor CURSOR FOR
    SELECT 
        OBJECT_NAME(ips.object_id) AS table_name,
        i.name AS index_name,
        ips.avg_fragmentation_in_percent
    FROM sys.dm_db_index_physical_stats(
        DB_ID(), NULL, NULL, NULL, 'LIMITED'
    ) ips
    INNER JOIN sys.indexes i 
        ON ips.object_id = i.object_id 
        AND ips.index_id = i.index_id
    WHERE ips.avg_fragmentation_in_percent > @fragmentation_threshold
    AND i.name IS NOT NULL  -- Exclude heaps
    AND OBJECT_NAME(ips.object_id) IN (
        'users', 'tags', 'workspaces', 
        'workspace_members', 'workspace_relationship_types',
        'workspace_items', 'entity_types',
        'notes', 'note_members', 'note_versions'
    )
    ORDER BY ips.avg_fragmentation_in_percent DESC;
    
    OPEN index_cursor;
    
    FETCH NEXT FROM index_cursor INTO @table_name, @index_name, @fragmentation;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'Rebuilding ' + @table_name + '.' + @index_name + 
              ' (Fragmentation: ' + CAST(@fragmentation AS NVARCHAR) + '%)';
        
        SET @sql = 'ALTER INDEX ' + QUOTENAME(@index_name) + 
                   ' ON ' + QUOTENAME(@table_name) + ' REBUILD';
        
        EXEC sp_executesql @sql;
        
        FETCH NEXT FROM index_cursor INTO @table_name, @index_name, @fragmentation;
    END;
    
    CLOSE index_cursor;
    DEALLOCATE index_cursor;
    
    PRINT 'Index rebuild completed';
END;
GO

PRINT '   ✅ usp_rebuild_fragmented_indexes created';

-- Procedure: Update statistics for all tables
GO

PRINT 'âœ… usp_rebuild_fragmented_indexes created successfully';
GO

