-- =============================================
-- CLEANUP SCRIPT: Drop note_tags table
-- Description: Remove deprecated note_tags table after migration to entity_tags
-- Author: Claude Code
-- Date: 2025-01-30
-- =============================================

PRINT '========================================';
PRINT 'CLEANUP: Dropping note_tags table';
PRINT 'Time: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
PRINT '========================================';
GO

-- =============================================
-- SAFETY CHECKS
-- =============================================
PRINT '';
PRINT 'Step 1: Safety checks...';

-- Check if entity_tags table exists
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
BEGIN
    PRINT '  ERROR: entity_tags table does not exist!';
    PRINT '  Migration may not have completed. Please run migration first.';
    RAISERROR('entity_tags table not found', 16, 1);
    RETURN;
END
ELSE
BEGIN
    PRINT '  ✓ entity_tags table exists';
END;

-- Check if note_tags table exists
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_tags')
BEGIN
    PRINT '  NOTE: note_tags table already dropped. Nothing to do.';
    RETURN;
END
ELSE
BEGIN
    PRINT '  ✓ note_tags table exists and will be dropped';
END;

-- Check data migration status
DECLARE @note_tags_count INT;
DECLARE @entity_tags_note_count INT;

SELECT @note_tags_count = COUNT(*) FROM note_tags;
SELECT @entity_tags_note_count = COUNT(*) FROM entity_tags WHERE entity_type = 'note';

PRINT '';
PRINT 'Step 2: Data verification...';
PRINT '  note_tags records: ' + CAST(@note_tags_count AS VARCHAR(10));
PRINT '  entity_tags (note) records: ' + CAST(@entity_tags_note_count AS VARCHAR(10));

-- WARNING if note_tags still has data that wasn't migrated
IF @note_tags_count > 0 AND @entity_tags_note_count = 0
BEGIN
    PRINT '';
    PRINT '  WARNING: note_tags has data but entity_tags is empty!';
    PRINT '  This suggests migration may not have run successfully.';
    PRINT '  Please verify migration before dropping note_tags.';
    PRINT '';
    RAISERROR('Migration verification failed - data not migrated', 16, 1);
    RETURN;
END;

-- =============================================
-- DROP NOTE_TAGS TABLE
-- =============================================
PRINT '';
PRINT 'Step 3: Dropping note_tags table...';

BEGIN TRY
    -- Drop foreign key constraints first
    DECLARE @constraint_name NVARCHAR(200);
    DECLARE @sql NVARCHAR(500);

    -- Find and drop all FK constraints on note_tags
    DECLARE constraint_cursor CURSOR FOR
    SELECT name
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('note_tags');

    OPEN constraint_cursor;
    FETCH NEXT FROM constraint_cursor INTO @constraint_name;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = 'ALTER TABLE note_tags DROP CONSTRAINT ' + @constraint_name;
        EXEC sp_executesql @sql;
        PRINT '  Dropped FK constraint: ' + @constraint_name;

        FETCH NEXT FROM constraint_cursor INTO @constraint_name;
    END;

    CLOSE constraint_cursor;
    DEALLOCATE constraint_cursor;

    -- Drop the table
    DROP TABLE note_tags;
    PRINT '  ✓ note_tags table dropped successfully';

END TRY
BEGIN CATCH
    PRINT '  ERROR: Failed to drop note_tags table';
    PRINT '  Error Message: ' + ERROR_MESSAGE();
    THROW;
END CATCH;

-- =============================================
-- FINAL VERIFICATION
-- =============================================
PRINT '';
PRINT 'Step 4: Final verification...';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_tags')
BEGIN
    PRINT '  ERROR: note_tags table still exists!';
END
ELSE
BEGIN
    PRINT '  ✓ note_tags table successfully removed';
END;

-- =============================================
-- SUMMARY
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'CLEANUP COMPLETED';
PRINT '========================================';
PRINT '';
PRINT 'Summary:';
PRINT '  note_tags table: DROPPED ✓';
PRINT '  entity_tags (note) records: ' + CAST(@entity_tags_note_count AS VARCHAR(10));
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Verify Note API still works correctly';
PRINT '  2. Test tag loading in frontend';
PRINT '  3. Remove NoteTag entity and configuration from C# code';
PRINT '';
PRINT 'Cleanup completed at: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
GO
