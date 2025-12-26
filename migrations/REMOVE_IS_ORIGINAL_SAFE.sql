-- =============================================
-- SAFELY REMOVE is_original column
-- Drop all dependencies first
-- =============================================
USE [SuperApp-dev];
GO

PRINT '=============================================';
PRINT 'SAFELY REMOVING is_original COLUMN';
PRINT 'Started at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '=============================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    PRINT 'Step 1: Dropping all indexes that reference is_original...';

    -- Drop ALL indexes that reference is_original column
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
        PRINT '  Dropping index: ' + @indexName;
        EXEC sp_executesql @sql;
        PRINT '  ✓ Dropped: ' + @indexName;

        FETCH NEXT FROM index_cursor INTO @indexName;
    END

    CLOSE index_cursor;
    DEALLOCATE index_cursor;

    PRINT '';
    PRINT 'Step 2: Dropping default constraints...';

    -- Drop default constraint if exists
    DECLARE @constraintName NVARCHAR(128);

    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('ws.workspace_items')
      AND c.name = 'is_original';

    IF @constraintName IS NOT NULL
    BEGIN
        SET @sql = 'ALTER TABLE ws.workspace_items DROP CONSTRAINT ' + QUOTENAME(@constraintName);
        PRINT '  Dropping constraint: ' + @constraintName;
        EXEC sp_executesql @sql;
        PRINT '  ✓ Dropped: ' + @constraintName;
    END
    ELSE
    BEGIN
        PRINT '  ○ No default constraint found';
    END

    PRINT '';
    PRINT 'Step 3: Dropping check constraints...';

    -- Drop check constraints
    DECLARE constraint_cursor CURSOR FOR
    SELECT c.name
    FROM sys.check_constraints c
    INNER JOIN sys.columns col ON c.parent_object_id = col.object_id
    WHERE col.object_id = OBJECT_ID('ws.workspace_items')
      AND col.name = 'is_original';

    OPEN constraint_cursor;
    FETCH NEXT FROM constraint_cursor INTO @constraintName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = 'ALTER TABLE ws.workspace_items DROP CONSTRAINT ' + QUOTENAME(@constraintName);
        PRINT '  Dropping constraint: ' + @constraintName;
        EXEC sp_executesql @sql;
        PRINT '  ✓ Dropped: ' + @constraintName;

        FETCH NEXT FROM constraint_cursor INTO @constraintName;
    END

    CLOSE constraint_cursor;
    DEALLOCATE constraint_cursor;

    PRINT '';
    PRINT 'Step 4: Dropping the is_original column...';

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
    BEGIN
        PRINT '  ○ Column is_original already removed';
    END

    COMMIT TRANSACTION;

    PRINT '';
    PRINT '=============================================';
    PRINT '✅ SUCCESSFULLY REMOVED is_original COLUMN!';
    PRINT 'Completed at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
    PRINT '=============================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '❌ ERROR: ' + ERROR_MESSAGE();
    PRINT 'Line: ' + CAST(ERROR_LINE() AS NVARCHAR(10));
    PRINT '';

    RAISERROR('Failed to remove is_original column', 16, 1);
END CATCH
GO

-- Verify removal
PRINT '';
PRINT 'Verifying removal...';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.workspace_items')
    AND name = 'is_original'
)
    PRINT '✅ Confirmed: is_original column removed';
ELSE
    PRINT '❌ ERROR: is_original column still exists!';

GO
