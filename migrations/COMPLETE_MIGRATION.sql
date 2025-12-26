-- =============================================
-- COMPLETE WORKSPACE MIGRATION
-- Step 1: Remove is_original (with dependencies)
-- Step 2: Add copy_info columns
-- Step 3: Add indexes
-- =============================================
USE [SuperApp-dev];
GO

PRINT '=============================================';
PRINT 'COMPLETE WORKSPACE MIGRATION';
PRINT 'Started at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '=============================================';
PRINT '';

-- =============================================
-- STEP 1: REMOVE is_original column (with all dependencies)
-- =============================================
PRINT '========================================';
PRINT 'STEP 1: Removing is_original column...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    PRINT 'Step 1.1: Dropping indexes...';

    DECLARE @sql NVARCHAR(MAX);
    DECLARE @indexName NVARCHAR(128);

    DECLARE index_cursor CURSOR FOR
    SELECT DISTINCT i.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    WHERE i.object_id = OBJECT_ID('ws.workspace_items')
      AND COL_NAME(ic.object_id, ic.column_id) = 'is_original';

    OPEN index_cursor;
    FETCH NEXT FROM index_cursor INTO @indexName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = 'DROP INDEX ' + QUOTENAME(@indexName) + ' ON ws.workspace_items';
        EXEC sp_executesql @sql;
        PRINT '  ✓ Dropped index: ' + @indexName;
        FETCH NEXT FROM index_cursor INTO @indexName;
    END

    CLOSE index_cursor;
    DEALLOCATE index_cursor;

    PRINT '';
    PRINT 'Step 1.2: Dropping default constraint...';

    DECLARE @constraintName NVARCHAR(128);

    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('ws.workspace_items')
      AND c.name = 'is_original';

    IF @constraintName IS NOT NULL
    BEGIN
        SET @sql = 'ALTER TABLE ws.workspace_items DROP CONSTRAINT ' + QUOTENAME(@constraintName);
        EXEC sp_executesql @sql;
        PRINT '  ✓ Dropped constraint: ' + @constraintName;
    END
    ELSE
        PRINT '  ○ No default constraint';

    PRINT '';
    PRINT 'Step 1.3: Dropping column...';

    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'is_original'
    )
    BEGIN
        ALTER TABLE ws.workspace_items DROP COLUMN is_original;
        PRINT '  ✓ Dropped column: is_original';
    END
    ELSE
        PRINT '  ○ Column already removed';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 1 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 1 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 1 failed', 16, 1);
END CATCH
PRINT '';

-- =============================================
-- STEP 2: ADD copy_info columns
-- =============================================
PRINT '========================================';
PRINT 'STEP 2: Adding copy_info columns...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    -- Notes
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.notes')
        AND name = 'copy_info'
    )
    BEGIN
        ALTER TABLE dbo.notes ADD copy_info NVARCHAR(MAX) NULL;
        PRINT '  ✓ Added: dbo.notes.copy_info';
    END
    ELSE
        PRINT '  ○ dbo.notes.copy_info exists';

    -- Files
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.files')
        AND name = 'copy_info'
    )
    BEGIN
        ALTER TABLE ws.files ADD copy_info NVARCHAR(MAX) NULL;
        PRINT '  ✓ Added: ws.files.copy_info';
    END
    ELSE
        PRINT '  ○ ws.files.copy_info exists';

    -- Folders
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.folders')
        AND name = 'copy_info'
    )
    BEGIN
        ALTER TABLE ws.folders ADD copy_info NVARCHAR(MAX) NULL;
        PRINT '  ✓ Added: ws.folders.copy_info';
    END
    ELSE
        PRINT '  ○ ws.folders.copy_info exists';

    -- Workspace Items
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'copy_info'
    )
    BEGIN
        ALTER TABLE ws.workspace_items ADD copy_info NVARCHAR(MAX) NULL;
        PRINT '  ✓ Added: ws.workspace_items.copy_info';
    END
    ELSE
        PRINT '  ○ ws.workspace_items.copy_info exists';

    -- Workspaces
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.workspaces')
        AND name = 'copy_info'
    )
    BEGIN
        ALTER TABLE ws.workspaces ADD copy_info NVARCHAR(MAX) NULL;
        PRINT '  ✓ Added: ws.workspaces.copy_info';
    END
    ELSE
        PRINT '  ○ ws.workspaces.copy_info exists';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 2 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 2 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 2 failed', 16, 1);
END CATCH
PRINT '';

-- =============================================
-- STEP 3: ADD performance indexes (optional)
-- =============================================
PRINT '========================================';
PRINT 'STEP 3: Adding indexes (optional)...';
PRINT '========================================';
PRINT '';

BEGIN TRY
    -- Note: Each index in separate try-catch to continue on error
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_notes_copy_info')
        BEGIN
            CREATE INDEX IX_notes_copy_info ON dbo.notes(copy_info) WHERE copy_info IS NOT NULL;
            PRINT '  ✓ Created: IX_notes_copy_info';
        END
        ELSE
            PRINT '  ○ IX_notes_copy_info exists';
    END TRY
    BEGIN CATCH
        PRINT '  ⚠ Skipped: IX_notes_copy_info (' + ERROR_MESSAGE() + ')';
    END CATCH

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_files_copy_info')
        BEGIN
            CREATE INDEX IX_files_copy_info ON ws.files(copy_info) WHERE copy_info IS NOT NULL;
            PRINT '  ✓ Created: IX_files_copy_info';
        END
        ELSE
            PRINT '  ○ IX_files_copy_info exists';
    END TRY
    BEGIN CATCH
        PRINT '  ⚠ Skipped: IX_files_copy_info (' + ERROR_MESSAGE() + ')';
    END CATCH

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_folders_copy_info')
        BEGIN
            CREATE INDEX IX_folders_copy_info ON ws.folders(copy_info) WHERE copy_info IS NOT NULL;
            PRINT '  ✓ Created: IX_folders_copy_info';
        END
        ELSE
            PRINT '  ○ IX_folders_copy_info exists';
    END TRY
    BEGIN CATCH
        PRINT '  ⚠ Skipped: IX_folders_copy_info (' + ERROR_MESSAGE() + ')';
    END CATCH

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_workspace_items_parent_lookup')
        BEGIN
            CREATE INDEX IX_workspace_items_parent_lookup
            ON ws.workspace_items(workspace_id, parent_id, deleted_at)
            WHERE deleted_at IS NULL;
            PRINT '  ✓ Created: IX_workspace_items_parent_lookup';
        END
        ELSE
            PRINT '  ○ IX_workspace_items_parent_lookup exists';
    END TRY
    BEGIN CATCH
        PRINT '  ⚠ Skipped: IX_workspace_items_parent_lookup (' + ERROR_MESSAGE() + ')';
    END CATCH

    PRINT '  ✅ Step 3 completed!';
END TRY
BEGIN CATCH
    PRINT '  ⚠ Step 3 had errors (non-critical)';
END CATCH
PRINT '';

-- =============================================
-- FINAL VERIFICATION
-- =============================================
PRINT '========================================';
PRINT 'FINAL VERIFICATION';
PRINT '========================================';
PRINT '';

-- Check is_original removed
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.workspace_items')
    AND name = 'is_original'
)
    PRINT '✅ is_original column removed';
ELSE
    PRINT '❌ ERROR: is_original still exists!';

-- Check copy_info columns
DECLARE @copyInfoCount INT;
SELECT @copyInfoCount = COUNT(*)
FROM sys.columns
WHERE name = 'copy_info'
  AND object_id IN (
      OBJECT_ID('dbo.notes'),
      OBJECT_ID('ws.files'),
      OBJECT_ID('ws.folders'),
      OBJECT_ID('ws.workspace_items'),
      OBJECT_ID('ws.workspaces')
  );

IF @copyInfoCount = 5
    PRINT '✅ All 5 copy_info columns added';
ELSE
    PRINT '⚠ Warning: Only ' + CAST(@copyInfoCount AS VARCHAR) + '/5 copy_info columns found';

PRINT '';
PRINT '========================================';
PRINT '✅ MIGRATION COMPLETED!';
PRINT 'Completed at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '========================================';
GO
