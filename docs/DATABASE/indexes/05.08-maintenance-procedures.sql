-- ============================================
-- FILE: indexes/05.08-maintenance-procedures.sql
-- PURPOSE: Index maintenance, fragmentation report, and query performance helpers
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql
-- ============================================

PRINT '🔧 Creating index maintenance and performance procedures...';
GO

-- Procedure: Rebuild fragmented indexes
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
CREATE OR ALTER PROCEDURE usp_update_all_statistics
AS
BEGIN
    SET NOCOUNT ON;
    
    PRINT 'Updating statistics...';
    
    UPDATE STATISTICS users WITH FULLSCAN;
    UPDATE STATISTICS tags WITH FULLSCAN;
    UPDATE STATISTICS workspaces WITH FULLSCAN;
    UPDATE STATISTICS workspace_members WITH FULLSCAN;
    UPDATE STATISTICS workspace_relationship_types WITH FULLSCAN;
    UPDATE STATISTICS workspace_items WITH FULLSCAN;
    UPDATE STATISTICS entity_types WITH FULLSCAN;
    UPDATE STATISTICS notes WITH FULLSCAN;
    UPDATE STATISTICS note_members WITH FULLSCAN;
    UPDATE STATISTICS note_versions WITH FULLSCAN;
    
    PRINT 'Statistics update completed';
END;
GO

PRINT '   ✅ usp_update_all_statistics created';

-- Procedure: Get index usage statistics
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
CREATE OR ALTER PROCEDURE usp_s_missing_indexes
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        OBJECT_NAME(mid.object_id) AS table_name,
        mid.equality_columns,
        mid.inequality_columns,
        mid.included_columns,
        migs.user_seeks,
        migs.user_scans,
        migs.avg_total_user_cost,
        migs.avg_user_impact,
        'CREATE INDEX ix_missing_' + 
        CAST(ROW_NUMBER() OVER (ORDER BY migs.avg_user_impact DESC) AS NVARCHAR) +
        ' ON ' + OBJECT_NAME(mid.object_id) + ' (' + 
        ISNULL(mid.equality_columns, '') +
        CASE 
            WHEN mid.inequality_columns IS NOT NULL 
            THEN CASE WHEN mid.equality_columns IS NOT NULL THEN ', ' ELSE '' END + mid.inequality_columns
            ELSE ''
        END + ')' +
        CASE 
            WHEN mid.included_columns IS NOT NULL 
            THEN ' INCLUDE (' + mid.included_columns + ')'
            ELSE ''
        END AS create_index_statement
    FROM sys.dm_db_missing_index_details mid
    INNER JOIN sys.dm_db_missing_index_groups mig 
        ON mid.index_handle = mig.index_handle
    INNER JOIN sys.dm_db_missing_index_group_stats migs 
        ON mig.index_group_handle = migs.group_handle
    WHERE mid.database_id = DB_ID()
    AND OBJECT_NAME(mid.object_id) IN (
        'users', 'tags', 'workspaces',
        'workspace_members', 'workspace_relationship_types',
        'workspace_items', 'entity_types',
        'notes', 'note_members', 'note_versions'
    )
    ORDER BY migs.avg_user_impact DESC;
END;
GO

PRINT '   ✅ usp_s_missing_indexes created';

-- Procedure: Get index fragmentation report
CREATE OR ALTER PROCEDURE usp_s_index_fragmentation
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        OBJECT_NAME(ips.object_id) AS table_name,
        i.name AS index_name,
        i.type_desc AS index_type,
        ips.avg_fragmentation_in_percent,
        ips.page_count,
        CASE 
            WHEN ips.avg_fragmentation_in_percent > 30 THEN 'REBUILD'
            WHEN ips.avg_fragmentation_in_percent > 10 THEN 'REORGANIZE'
            ELSE 'OK'
        END AS recommended_action
    FROM sys.dm_db_index_physical_stats(
        DB_ID(), NULL, NULL, NULL, 'LIMITED'
    ) ips
    INNER JOIN sys.indexes i 
        ON ips.object_id = i.object_id 
        AND ips.index_id = i.index_id
    WHERE i.name IS NOT NULL
    AND OBJECT_NAME(ips.object_id) IN (
        'users', 'tags', 'workspaces',
        'workspace_members', 'workspace_relationship_types',
        'workspace_items', 'entity_types',
        'notes', 'note_members', 'note_versions'
    )
    ORDER BY 
        ips.avg_fragmentation_in_percent DESC,
        table_name;
END;
GO

PRINT '   ✅ usp_s_index_fragmentation created';

-- Procedure: Get slow queries
CREATE OR ALTER PROCEDURE usp_s_slow_queries
    @top INT = 20,
    @min_avg_duration_ms INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@top)
        qs.execution_count,
        qs.total_elapsed_time / 1000 / 1000 AS total_elapsed_seconds,
        qs.total_elapsed_time / 1000 / qs.execution_count AS avg_elapsed_ms,
        qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
        qs.total_physical_reads / qs.execution_count AS avg_physical_reads,
        qs.creation_time,
        qs.last_execution_time,
        SUBSTRING(
            st.text,
            (qs.statement_start_offset / 2) + 1,
            (
                CASE qs.statement_end_offset
                    WHEN -1 THEN DATALENGTH(st.text)
                    ELSE qs.statement_end_offset
                END - qs.statement_start_offset
            ) / 2 + 1
        ) AS query_text,
        qp.query_plan
    FROM sys.dm_exec_query_stats qs
    CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
    CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
    WHERE qs.total_elapsed_time / 1000 / qs.execution_count > @min_avg_duration_ms
    ORDER BY avg_elapsed_ms DESC;
END;
GO

PRINT '   ✅ usp_s_slow_queries created';

-- Procedure: Get table sizes
CREATE OR ALTER PROCEDURE usp_s_table_sizes
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        t.name AS table_name,
        SUM(p.rows) AS row_count,
        SUM(a.total_pages) * 8 / 1024 AS total_space_mb,
        SUM(a.used_pages) * 8 / 1024 AS used_space_mb,
        (SUM(a.total_pages) - SUM(a.used_pages)) * 8 / 1024 AS unused_space_mb
    FROM sys.tables t
    INNER JOIN sys.partitions p ON t.object_id = p.object_id
    INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
    WHERE t.name IN (
        'users', 'tags', 'workspaces',
        'workspace_members', 'workspace_relationship_types',
        'workspace_items', 'entity_types',
        'notes', 'note_members', 'note_versions'
    )
    AND p.index_id IN (0, 1)  -- Heap or clustered index
    GROUP BY t.name
    ORDER BY total_space_mb DESC;
END;
GO

PRINT '   ✅ usp_s_table_sizes created';

-- List all indexes
SELECT 
    OBJECT_NAME(i.object_id) AS table_name,
    i.name AS index_name,
    i.type_desc AS index_type,
    i.is_unique,
    i.fill_factor,
    STUFF((
        SELECT ', ' + COL_NAME(ic.object_id, ic.column_id)
        FROM sys.index_columns ic
        WHERE ic.object_id = i.object_id
        AND ic.index_id = i.index_id
        AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS key_columns,
    STUFF((
        SELECT ', ' + COL_NAME(ic.object_id, ic.column_id)
        FROM sys.index_columns ic
        WHERE ic.object_id = i.object_id
        AND ic.index_id = i.index_id
        AND ic.is_included_column = 1
        ORDER BY ic.index_column_id
        FOR XML PATH('')
    ), 1, 2, '') AS included_columns
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN (
    'users', 'tags', 'workspaces',
    'workspace_members', 'workspace_relationship_types',
    'workspace_items', 'entity_types',
    'notes', 'note_members', 'note_versions'
)
AND i.type > 0  -- Exclude heaps
ORDER BY table_name, index_name;

GO

PRINT '✅ Index maintenance and performance procedures created successfully!';
PRINT '📊 Next step: Run indexes/05.09-maintenance-recommendations.sql';
GO