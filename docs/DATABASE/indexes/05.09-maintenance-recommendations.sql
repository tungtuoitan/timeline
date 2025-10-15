-- ============================================
-- FILE: indexes/05.09-maintenance-recommendations.sql
-- PURPOSE: Index maintenance schedule recommendations
-- DEPENDENCIES: indexes/05.08-maintenance-procedures.sql
-- ============================================

PRINT '🔧 Index maintenance recommendations';
GO

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

PRINT '✅ Index maintenance recommendations documented!';
PRINT '📊 Next step: Run 06-constraints.sql';
GO