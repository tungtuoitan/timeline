-- =============================================
-- WORKSPACE SCHEMA UPDATE (FIXED)
-- Remove is_original, Add copy_info
-- =============================================
-- Author: SuperApp Team
-- Date: 2025-12-25
-- Description: Simplify workspace_items and add copy tracking
-- =============================================

USE [SuperApp-dev];
GO

PRINT '=============================================';
PRINT 'WORKSPACE SCHEMA UPDATE';
PRINT 'Started at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '=============================================';
PRINT '';

-- =============================================
-- STEP 1: REMOVE is_original column (separate transaction)
-- =============================================
PRINT 'Step 1: Removing is_original column...';

BEGIN TRANSACTION;
BEGIN TRY
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'is_original'
    )
    BEGIN
        -- Drop index first if exists
        IF EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_workspace_items_original'
                   AND object_id = OBJECT_ID('ws.workspace_items'))
        BEGIN
            DROP INDEX IX_workspace_items_original ON ws.workspace_items;
            PRINT '  ✓ Dropped index: IX_workspace_items_original';
        END

        -- Drop column
        ALTER TABLE ws.workspace_items DROP COLUMN is_original;
        PRINT '  ✓ Dropped column: is_original';
    END
    ELSE
    BEGIN
        PRINT '  ○ Column is_original already removed';
    END

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 1 completed successfully!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 1 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 1 failed', 16, 1);
END CATCH
PRINT '';

-- =============================================
-- STEP 2: ADD copy_info columns (separate transaction)
-- =============================================
PRINT 'Step 2: Adding copy_info columns...';

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
        PRINT '  ○ dbo.notes.copy_info already exists';

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
        PRINT '  ○ ws.files.copy_info already exists';

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
        PRINT '  ○ ws.folders.copy_info already exists';

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
        PRINT '  ○ ws.workspace_items.copy_info already exists';

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
        PRINT '  ○ ws.workspaces.copy_info already exists';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 2 completed successfully!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 2 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 2 failed', 16, 1);
END CATCH
PRINT '';

-- =============================================
-- STEP 3: ADD performance indexes (separate transaction)
-- =============================================
PRINT 'Step 3: Adding performance indexes...';

BEGIN TRANSACTION;
BEGIN TRY
    -- Index for finding copied notes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_notes_copy_info' AND object_id = OBJECT_ID('dbo.notes'))
    BEGIN
        CREATE INDEX IX_notes_copy_info ON dbo.notes(copy_info)
        WHERE copy_info IS NOT NULL;
        PRINT '  ✓ Created: IX_notes_copy_info';
    END
    ELSE
        PRINT '  ○ IX_notes_copy_info already exists';

    -- Index for finding copied files
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_files_copy_info' AND object_id = OBJECT_ID('ws.files'))
    BEGIN
        CREATE INDEX IX_files_copy_info ON ws.files(copy_info)
        WHERE copy_info IS NOT NULL;
        PRINT '  ✓ Created: IX_files_copy_info';
    END
    ELSE
        PRINT '  ○ IX_files_copy_info already exists';

    -- Index for finding copied folders
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_folders_copy_info' AND object_id = OBJECT_ID('ws.folders'))
    BEGIN
        CREATE INDEX IX_folders_copy_info ON ws.folders(copy_info)
        WHERE copy_info IS NOT NULL;
        PRINT '  ✓ Created: IX_folders_copy_info';
    END
    ELSE
        PRINT '  ○ IX_folders_copy_info already exists';

    -- Improve parent lookup performance
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_workspace_items_parent_lookup' AND object_id = OBJECT_ID('ws.workspace_items'))
    BEGIN
        CREATE INDEX IX_workspace_items_parent_lookup
        ON ws.workspace_items(workspace_id, parent_id, deleted_at)
        WHERE deleted_at IS NULL;
        PRINT '  ✓ Created: IX_workspace_items_parent_lookup';
    END
    ELSE
        PRINT '  ○ IX_workspace_items_parent_lookup already exists';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 3 completed successfully!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 3 failed: ' + ERROR_MESSAGE();
    -- Don't throw error, indexes are optional
    PRINT '  ⚠ Continuing without indexes...';
END CATCH
PRINT '';

-- =============================================
-- FINAL VERIFICATION
-- =============================================
PRINT '';
PRINT '=============================================';
PRINT 'FINAL VERIFICATION';
PRINT '=============================================';
PRINT '';

-- Check removed column
PRINT 'Checking is_original removal...';
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.workspace_items')
    AND name = 'is_original'
)
    PRINT '  ✅ Confirmed: is_original removed';
ELSE
    PRINT '  ❌ ERROR: is_original still exists!';

PRINT '';
PRINT 'Checking copy_info columns...';

-- Check added columns
SELECT
    SCHEMA_NAME(t.schema_id) + '.' + t.name AS table_name,
    c.name AS column_name,
    ty.name AS data_type,
    CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length AS VARCHAR) END AS max_length
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.name = 'copy_info'
  AND t.name IN ('notes', 'files', 'folders', 'workspace_items', 'workspaces')
ORDER BY t.name;

PRINT '';
PRINT 'Checking indexes...';

-- Check indexes
SELECT
    SCHEMA_NAME(t.schema_id) + '.' + t.name AS table_name,
    i.name AS index_name,
    i.type_desc
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.name LIKE '%copy_info%'
   OR i.name LIKE '%parent_lookup%'
ORDER BY t.name, i.name;

PRINT '';
PRINT '=============================================';
PRINT '✅ MIGRATION COMPLETED!';
PRINT 'Completed at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '=============================================';
GO
