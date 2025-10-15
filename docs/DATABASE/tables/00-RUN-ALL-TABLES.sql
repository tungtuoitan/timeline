-- ============================================
-- FILE: tables/00-RUN-ALL-TABLES.sql
-- PURPOSE: Execute all table creation scripts in correct order
-- DESCRIPTION: Master script to create all database tables
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '';
PRINT '═══════════════════════════════════════════════════════════';
PRINT '   SUPERAPP DATABASE - TABLE CREATION';
PRINT '   Version: 3.0 (Unified System)';
PRINT '   Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '═══════════════════════════════════════════════════════════';
PRINT '';

-- ============================================
-- SECTION 1: CORE TABLES
-- ============================================

PRINT '📦 Section 1: Creating core tables...';
PRINT '';

:r core\users.sql
:r core\tags.sql

PRINT '';
PRINT '   ✅ Core tables completed';
PRINT '';

-- ============================================
-- SECTION 2: WORKSPACE TABLES
-- ============================================

PRINT '📦 Section 2: Creating workspace tables...';
PRINT '';

:r workspace\workspaces.sql
:r workspace\workspace_members.sql
:r workspace\workspace_relationship_types.sql

PRINT '';
PRINT '   ✅ Workspace tables completed';
PRINT '';

-- ============================================
-- SECTION 3: ENTITY TABLES (Unified System)
-- ============================================

PRINT '📦 Section 3: Creating entity tables (Unified System)...';
PRINT '';

:r entities\entity_types.sql
:r entities\workspace_items.sql
:r entities\notes.sql
:r entities\note_versions.sql
:r entities\note_members.sql
:r entities\views.sql

PRINT '';
PRINT '   ✅ Entity tables completed';
PRINT '';

-- ============================================
-- SECTION 4: ADDITIONAL INDEXES
-- ============================================

PRINT '📦 Section 4: Creating additional indexes...';
PRINT '';

:r indexes\composite_indexes.sql
:r indexes\covering_indexes.sql
:r indexes\workspace_items_indexes.sql
:r indexes\notes_indexes.sql

PRINT '';
PRINT '   ✅ Additional indexes completed';
PRINT '';

-- ============================================
-- SECTION 5: CONSTRAINTS & VALIDATION
-- ============================================

PRINT '📦 Section 5: Adding constraints and validation...';
PRINT '';

:r constraints\user_constraints.sql
:r constraints\tag_constraints.sql
:r constraints\workspace_constraints.sql
:r constraints\workspace_items_constraints.sql
:r constraints\notes_constraints.sql

PRINT '';
PRINT '   ✅ Constraints completed';
PRINT '';

-- ============================================
-- SECTION 6: AUDIT SYSTEM
-- ============================================

PRINT '📦 Section 6: Creating audit system...';
PRINT '';

:r audit\audit_log_table.sql
:r audit\workspace_member_audit.sql
:r audit\note_sharing_audit.sql
:r audit\item_movement_audit.sql
:r audit\audit_procedures.sql

PRINT '';
PRINT '   ✅ Audit system completed';
PRINT '';

-- ============================================
-- SECTION 7: CACHE SYSTEM (Performance)
-- ============================================

PRINT '📦 Section 7: Creating cache system...';
PRINT '';

:r cache\workspace_tree_cache.sql
:r cache\cache_refresh_procedures.sql
:r cache\cache_query_procedures.sql
:r cache\cache_maintenance.sql
:r cache\cache_triggers.sql

PRINT '';
PRINT '   ✅ Cache system completed';
PRINT '';

-- ============================================
-- FINAL VALIDATION
-- ============================================

PRINT '';
PRINT '📊 Validating installation...';
PRINT '';

-- Count tables
DECLARE @table_count INT;
SELECT @table_count = COUNT(*) 
FROM sys.tables 
WHERE name IN (
    'users', 'tags', 
    'workspaces', 'workspace_members', 'workspace_relationship_types',
    'entity_types', 'workspace_items', 'notes', 'note_versions', 'note_members',
    'audit_log', 'workspace_tree_cache'
);

PRINT '   Tables created: ' + CAST(@table_count AS VARCHAR) + ' / 12';

-- Count indexes
DECLARE @index_count INT;
SELECT @index_count = COUNT(*) 
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN (
    'users', 'tags', 'workspaces', 'workspace_members', 
    'workspace_relationship_types', 'entity_types', 'workspace_items',
    'notes', 'note_versions', 'note_members', 'audit_log', 'workspace_tree_cache'
)
AND i.type > 0; -- Exclude heap

PRINT '   Indexes created: ' + CAST(@index_count AS VARCHAR);

-- Count procedures
DECLARE @proc_count INT;
SELECT @proc_count = COUNT(*) 
FROM sys.procedures
WHERE name LIKE 'usp_%';

PRINT '   Procedures created: ' + CAST(@proc_count AS VARCHAR);

-- Count triggers
DECLARE @trigger_count INT;
SELECT @trigger_count = COUNT(*) 
FROM sys.triggers t
INNER JOIN sys.tables tb ON t.parent_id = tb.object_id
WHERE tb.name IN (
    'users', 'tags', 'workspaces', 'workspace_members',
    'workspace_items', 'notes', 'note_members'
);

PRINT '   Triggers created: ' + CAST(@trigger_count AS VARCHAR);

PRINT '';
PRINT '═══════════════════════════════════════════════════════════';
PRINT '   ✅ DATABASE TABLE CREATION COMPLETE!';
PRINT '═══════════════════════════════════════════════════════════';
PRINT '';
PRINT '📝 Next Steps:';
PRINT '   1. Run procedures\00-RUN-ALL-PROCEDURES.sql';
PRINT '   2. Optional: Run 13-sample-data.sql for test data';
PRINT '   3. Verify installation: SELECT * FROM vw_active_users;';
PRINT '';
GO
