-- =============================================
-- Database Status Check Script
-- Check which tables exist and migration status
-- =============================================

PRINT '========================================';
PRINT 'DATABASE STATUS CHECK';
PRINT 'Time: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
PRINT '========================================';
PRINT '';

-- Check all relevant tables
PRINT 'TABLE EXISTENCE CHECK:';
PRINT '----------------------';

-- Old schema
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags')
    PRINT '  tags: EXISTS (old table, should be renamed to folders)'
ELSE
    PRINT '  tags: NOT FOUND';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_tags')
    PRINT '  note_tags: EXISTS (old table, should migrate to entity_tags)'
ELSE
    PRINT '  note_tags: NOT FOUND';

-- New schema
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'folders')
    PRINT '  folders: EXISTS ✓ (renamed from tags)'
ELSE
    PRINT '  folders: NOT FOUND (need to run migration)';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
    PRINT '  tags_new: EXISTS ✓ (hashtags table)'
ELSE
    PRINT '  tags_new: NOT FOUND (need to run migration)';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
    PRINT '  entity_tags: EXISTS ✓ (new polymorphic tagging)'
ELSE
    PRINT '  entity_tags: NOT FOUND (need to run migration)';

-- Other important tables
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_types')
    PRINT '  entity_types: EXISTS ✓'
ELSE
    PRINT '  entity_types: NOT FOUND';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspace_items')
    PRINT '  workspace_items: EXISTS ✓'
ELSE
    PRINT '  workspace_items: NOT FOUND';

PRINT '';
PRINT 'RECORD COUNTS:';
PRINT '--------------';

-- Count records if tables exist
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags')
BEGIN
    DECLARE @tags_count INT;
    SELECT @tags_count = COUNT(*) FROM tags;
    PRINT '  tags: ' + CAST(@tags_count AS VARCHAR(10)) + ' records';
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_tags')
BEGIN
    DECLARE @note_tags_count INT;
    SELECT @note_tags_count = COUNT(*) FROM note_tags;
    PRINT '  note_tags: ' + CAST(@note_tags_count AS VARCHAR(10)) + ' records';
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'folders')
BEGIN
    DECLARE @folders_count INT;
    SELECT @folders_count = COUNT(*) FROM folders;
    PRINT '  folders: ' + CAST(@folders_count AS VARCHAR(10)) + ' records';
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
BEGIN
    DECLARE @tags_new_count INT;
    SELECT @tags_new_count = COUNT(*) FROM tags_new;
    PRINT '  tags_new: ' + CAST(@tags_new_count AS VARCHAR(10)) + ' records';
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
BEGIN
    DECLARE @entity_tags_count INT;
    SELECT @entity_tags_count = COUNT(*) FROM entity_tags;
    PRINT '  entity_tags: ' + CAST(@entity_tags_count AS VARCHAR(10)) + ' records';
END;

PRINT '';
PRINT 'MIGRATION STATUS:';
PRINT '----------------';

-- Determine migration status
DECLARE @migration_status VARCHAR(100);

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'folders')
   AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
   AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
BEGIN
    SET @migration_status = 'NOT STARTED - Need to run MIGRATION_TAGS_TO_FOLDERS_WITH_SHARE_FORK.sql';
    PRINT '  Status: ❌ ' + @migration_status;
    PRINT '';
    PRINT 'NEXT STEPS:';
    PRINT '  1. Run: MIGRATION_TAGS_TO_FOLDERS_WITH_SHARE_FORK.sql';
    PRINT '  2. Run: MIGRATION_PATCH_NOTE_TAGS_TO_ENTITY_TAGS.sql';
    PRINT '  3. Run: CLEANUP_DROP_NOTE_TAGS.sql';
END
ELSE IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'folders')
        AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
        AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
        AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_tags')
BEGIN
    SET @migration_status = 'PARTIALLY COMPLETE - entity_tags created but note_tags still exists';
    PRINT '  Status: ⚠️ ' + @migration_status;
    PRINT '';
    PRINT 'NEXT STEPS:';
    PRINT '  1. Run: MIGRATION_PATCH_NOTE_TAGS_TO_ENTITY_TAGS.sql (to migrate data)';
    PRINT '  2. Run: CLEANUP_DROP_NOTE_TAGS.sql (to drop old table)';
END
ELSE IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'folders')
        AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
        AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
        AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_tags')
BEGIN
    SET @migration_status = 'COMPLETE - All migrations applied';
    PRINT '  Status: ✅ ' + @migration_status;
    PRINT '';
    PRINT 'Database is up to date!';
END
ELSE
BEGIN
    SET @migration_status = 'UNKNOWN STATE - Please check manually';
    PRINT '  Status: ⚠️ ' + @migration_status;
END;

PRINT '';
PRINT '========================================';
PRINT 'END OF STATUS CHECK';
PRINT '========================================';
GO
