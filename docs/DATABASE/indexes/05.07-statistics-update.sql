-- ============================================
-- FILE: indexes/05.07-statistics-update.sql
-- PURPOSE: Update statistics for all tables
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql
-- ============================================

PRINT '🔧 Updating statistics for all tables...';
GO

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

PRINT '✅ Statistics updated for all tables!';
PRINT '📊 Next step: Run indexes/05.08-maintenance-procedures.sql';
GO