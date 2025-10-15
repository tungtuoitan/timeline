-- ============================================
-- FILE: relationships/04.06-validation-queries.sql
-- PURPOSE: ⚠️ DEPRECATED - Validation and performance test queries
-- DEPENDENCIES: relationships/04.01-table.sql, relationships/04.02-indexes.sql, relationships/04.04-views.sql (do not use)
-- STATUS: Kept for reference only, replaced by workspace_items in 07-tables-entities.sql
-- ============================================

-- ⚠️⚠️⚠️ WARNING ⚠️⚠️⚠️
-- This file is DEPRECATED and should NOT be executed.
-- Replaced by: workspace_items (07-tables-entities.sql)
-- Migration: See 12-migration-guide.md

-- Verify table created
SELECT 
    name AS table_name,
    create_date,
    modify_date
FROM sys.tables
WHERE name = 'workspace_tag_relationships';
GO

-- Verify indexes
SELECT 
    i.name AS index_name,
    i.type_desc,
    i.is_unique,
    COL_NAME(ic.object_id, ic.column_id) AS column_name
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('workspace_tag_relationships')
ORDER BY i.index_id, ic.key_ordinal;
GO

-- Verify triggers
SELECT 
    name AS trigger_name,
    create_date,
    modify_date
FROM sys.triggers
WHERE parent_id = OBJECT_ID('workspace_tag_relationships')
ORDER BY name;
GO

-- Verify views
SELECT 
    name AS view_name,
    create_date
FROM sys.views
WHERE name LIKE 'vw_%relationship%' OR name LIKE 'vw_%tag%'
ORDER BY name;
GO

-- Test 1: Get workspace tree (should use ix_wsrel_workspace)
/*
EXPLAIN
SELECT * FROM workspace_tag_relationships
WHERE workspace_id = 1 AND deleted_at IS NULL
ORDER BY to_path;
*/

-- Test 2: Get children of tag (should use ix_wsrel_from_tag)
/*
EXPLAIN
SELECT * FROM workspace_tag_relationships
WHERE workspace_id = 1 AND from_tag_id = 10 AND deleted_at IS NULL
ORDER BY sort_order;
*/

-- Test 3: Get subtree (should use ix_wsrel_path)
/*
EXPLAIN
SELECT * FROM workspace_tag_relationships
WHERE workspace_id = 1 AND to_path LIKE '1.5.%' AND deleted_at IS NULL;
*/

PRINT '⚠️ DEPRECATED: Validation and test queries created for reference only!';
PRINT '   Replaced by: workspace_items in 07-tables-entities.sql';
PRINT '   Migration: See 12-migration-guide.md';
PRINT '📊 Next step: Run relationships/04.07-deprecation-notice.sql (for reference only)';
GO