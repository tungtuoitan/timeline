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

GO

-- ============================================
-- SECTION 8: QUERY PERFORMANCE HELPERS
-- ============================================

PRINT '';
PRINT '📊 Section 8: Creating query performance helper procedures';

-- Procedure: Get slow queries
GO

PRINT 'âœ… usp_s_index_fragmentation created successfully';
GO

