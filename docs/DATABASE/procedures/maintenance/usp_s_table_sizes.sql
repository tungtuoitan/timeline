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

GO

-- ============================================
-- SECTION 9: VERIFY ALL INDEXES
-- ============================================

PRINT '';
PRINT '📊 Section 9: Verifying all indexes';

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

-- ============================================
-- SECTION 10: INDEX MAINTENANCE SCHEDULE
-- ============================================

PRINT '';
PRINT '📊 Section 10: Index maintenance recommendations';
PRINT '';
PRINT '╔══════════════════════════════════════════════════════════════╗';
PRINT '║  INDEX MAINTENANCE SCHEDULE RECOMMENDATIONS                  ║';
PRINT '╠══════════════════════════════════════════════════════════════╣';
PRINT '║                                                              ║';
PRINT '║  DAILY (off-peak hours):                                     ║';
PRINT '║    - Update statistics: EXEC usp_update_all_statistics       ║';
PRINT '║                                                              ║';
PRINT '║  WEEKLY (Sunday night):                                      ║';
PRINT '║    - Rebuild fragmented indexes:                             ║';
PRINT '║      EXEC usp_rebuild_fragmented_indexes @threshold=30       ║';
PRINT '║                                                              ║';
PRINT '║  MONTHLY:                                                    ║';
PRINT '║    - Check missing indexes: EXEC usp_s_missing_indexes       ║';
PRINT '║    - Review slow queries: EXEC usp_s_slow_queries            ║';
PRINT '║    - Check table sizes: EXEC usp_s_table_sizes               ║';
PRINT '║                                                              ║';
PRINT '║  AS NEEDED:                                                  ║';
PRINT '║    - Check fragmentation: EXEC usp_s_index_fragmentation     ║';
PRINT '║    - Review index usage: EXEC usp_s_index_usage_stats        ║';
PRINT '║                                                              ║';
PRINT '╚══════════════════════════════════════════════════════════════╝';

GO

-- ============================================
-- SUCCESS MESSAGE & SUMMARY
-- ============================================

PRINT '';
PRINT '✅ All indexes created successfully!';
PRINT '';
PRINT '📊 INDEX SUMMARY (Unified System):';
PRINT '   - Section 1: 6 composite indexes (users, tags, workspaces)';
PRINT '   - Section 2A: 3 covering indexes (workspace access)';
PRINT '   - Section 2B: 6 workspace_items indexes (hierarchy queries)';
PRINT '   - Section 2C: 6 notes system indexes (note CRUD & sharing)';
PRINT '   - Section 3: 9 filtered indexes (root items, tags/notes only, archived/pinned/favorite)';
PRINT '   - Section 4: Full-text search setup (commented, includes notes)';
PRINT '   - Section 5: Statistics updated for all tables';
PRINT '   - Section 6: 4 maintenance procedures';
PRINT '   - Section 7: Fragmentation report';
PRINT '   - Section 8: 2 performance helpers';
PRINT '';
PRINT '🛠️  MAINTENANCE PROCEDURES AVAILABLE:';
PRINT '   - usp_rebuild_fragmented_indexes (all tables)';
PRINT '   - usp_update_all_statistics (including workspace_items, notes)';
PRINT '   - usp_s_index_usage_stats (unified system tables)';
PRINT '   - usp_s_missing_indexes (unified system tables)';
PRINT '   - usp_s_index_fragmentation (all tables)';
PRINT '   - usp_s_slow_queries (query performance analysis)';
PRINT '   - usp_s_table_sizes (all tables including notes)';
PRINT '';
PRINT '📋 COVERED TABLES:';
PRINT '   Core: users, tags, workspaces, workspace_members, workspace_relationship_types';
PRINT '   Unified System: workspace_items, entity_types';
PRINT '   Notes System: notes, note_members, note_versions';
PRINT '';
PRINT '⚡ PERFORMANCE TARGETS:';
PRINT '   - Tag hierarchy queries: <50ms (via item_path + depth indexes)';
PRINT '   - Note access checks: <10ms (via note_members permissions index)';
PRINT '   - Full workspace tree: <300ms (via covering indexes)';
PRINT '   - Search queries: <200ms (via search indexes + full-text)';
PRINT '';
PRINT '📊 Next step: Run 06-constraints.sql';
GO
