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

GO

-- ============================================
-- SECTION 7: INDEX FRAGMENTATION REPORT
-- ============================================

PRINT '';
PRINT '📊 Section 7: Creating index fragmentation report procedure';
GO

PRINT 'âœ… usp_s_missing_indexes created successfully';
GO

