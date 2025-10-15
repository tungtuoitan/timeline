CREATE OR ALTER PROCEDURE usp_s_index_usage_stats
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        OBJECT_NAME(s.object_id) AS table_name,
        i.name AS index_name,
        i.type_desc AS index_type,
        s.user_seeks,
        s.user_scans,
        s.user_lookups,
        s.user_updates,
        s.user_seeks + s.user_scans + s.user_lookups AS total_reads,
        CASE 
            WHEN s.user_updates > 0 
            THEN CAST((s.user_seeks + s.user_scans + s.user_lookups) AS FLOAT) / s.user_updates
            ELSE NULL 
        END AS reads_per_write,
        s.last_user_seek,
        s.last_user_scan,
        s.last_user_lookup
    FROM sys.dm_db_index_usage_stats s
    INNER JOIN sys.indexes i 
        ON s.object_id = i.object_id 
        AND s.index_id = i.index_id
    WHERE OBJECT_NAME(s.object_id) IN (
        'users', 'tags', 'workspaces',
        'workspace_members', 'workspace_relationship_types',
        'workspace_items', 'entity_types',
        'notes', 'note_members', 'note_versions'
    )
    AND s.database_id = DB_ID()
    ORDER BY 
        table_name,
        total_reads DESC;
END;
GO

PRINT '   ✅ usp_s_index_usage_stats created';

-- Procedure: Get missing index recommendations
GO

PRINT 'âœ… usp_s_index_usage_stats created successfully';
GO

