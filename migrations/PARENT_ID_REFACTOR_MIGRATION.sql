-- =============================================
-- WORKSPACE PARENT_ID REFACTOR MIGRATION
-- =============================================
-- PURPOSE: Fix parent_id to reference workspace_items.id instead of folders.id
--          Rename item_id → entity_id, item_type → entity_type for clarity
--
-- CHANGES:
-- 1. Rename item_id → entity_id
-- 2. Rename item_type → entity_type
-- 3. Change parent_id FK: folders.id → workspace_items.id (self-referencing)
-- 4. Migrate data: Convert parent_id from entity ID to workspace_items.id
--
-- RUN ON: SuperApp-dev database
-- AUTHOR: Claude Code
-- DATE: 2025-12-28
-- =============================================

USE [SuperApp-dev];
GO

PRINT '=============================================';
PRINT 'WORKSPACE PARENT_ID REFACTOR MIGRATION';
PRINT 'Started at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '=============================================';
PRINT '';

-- =============================================
-- STEP 1: DROP EXISTING FOREIGN KEY CONSTRAINTS
-- =============================================
PRINT '========================================';
PRINT 'STEP 1: Dropping existing FK constraints...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    -- Drop FK to folders (parent_id → folders.id)
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @constraintName NVARCHAR(128);

    -- Find FK constraint on parent_id
    SELECT @constraintName = fk.name
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE fk.parent_object_id = OBJECT_ID('ws.workspace_items')
      AND c.name = 'parent_id';

    IF @constraintName IS NOT NULL
    BEGIN
        SET @sql = 'ALTER TABLE ws.workspace_items DROP CONSTRAINT ' + QUOTENAME(@constraintName);
        EXEC sp_executesql @sql;
        PRINT '  ✓ Dropped FK constraint: ' + @constraintName;
    END
    ELSE
        PRINT '  ○ No FK constraint on parent_id';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 1 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 1 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 1 failed', 16, 1);
END CATCH
PRINT '';
GO

-- =============================================
-- STEP 2: ADD TEMPORARY COLUMN FOR DATA MIGRATION
-- =============================================
PRINT '========================================';
PRINT 'STEP 2: Adding temporary column...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    -- Add temp column to store new parent_id (workspace_items.id)
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'parent_workspace_item_id'
    )
    BEGIN
        ALTER TABLE ws.workspace_items ADD parent_workspace_item_id INT NULL;
        PRINT '  ✓ Added: parent_workspace_item_id';
    END
    ELSE
        PRINT '  ○ parent_workspace_item_id exists';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 2 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 2 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 2 failed', 16, 1);
END CATCH
PRINT '';
GO

-- =============================================
-- STEP 3: MIGRATE DATA (parent_id: entity_id → workspace_items.id)
-- =============================================
PRINT '========================================';
PRINT 'STEP 3: Migrating parent_id data...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    -- Update parent_workspace_item_id by finding workspace_items.id for parent entity
    UPDATE child
    SET child.parent_workspace_item_id = parent.id
    FROM ws.workspace_items child
    INNER JOIN ws.workspace_items parent
        ON child.workspace_id = parent.workspace_id  -- Same workspace
        AND child.parent_id = parent.item_id          -- child.parent_id = parent's entity_id
    WHERE child.parent_id IS NOT NULL;

    DECLARE @migratedCount INT = @@ROWCOUNT;
    PRINT '  ✓ Migrated ' + CAST(@migratedCount AS NVARCHAR(10)) + ' parent_id references';

    -- Verify no orphans (parent_id exists but no matching workspace_item)
    DECLARE @orphanCount INT;
    SELECT @orphanCount = COUNT(*)
    FROM ws.workspace_items
    WHERE parent_id IS NOT NULL
      AND parent_workspace_item_id IS NULL;

    IF @orphanCount > 0
    BEGIN
        PRINT '  ⚠️  WARNING: ' + CAST(@orphanCount AS NVARCHAR(10)) + ' orphan records found!';
        PRINT '  These records have parent_id but no matching workspace_item in same workspace';

        -- Show orphan details
        SELECT
            id as workspace_item_id,
            workspace_id,
            parent_id as old_parent_entity_id,
            item_type,
            item_id as entity_id
        FROM ws.workspace_items
        WHERE parent_id IS NOT NULL
          AND parent_workspace_item_id IS NULL;
    END
    ELSE
        PRINT '  ✓ No orphan records';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 3 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 3 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 3 failed', 16, 1);
END CATCH
PRINT '';
GO

-- =============================================
-- STEP 4: REPLACE parent_id WITH parent_workspace_item_id
-- =============================================
PRINT '========================================';
PRINT 'STEP 4: Replacing parent_id column...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    -- Drop old parent_id column
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'parent_id'
    )
    BEGIN
        ALTER TABLE ws.workspace_items DROP COLUMN parent_id;
        PRINT '  ✓ Dropped old column: parent_id';
    END

    -- Rename temp column to parent_id
    EXEC sp_rename 'ws.workspace_items.parent_workspace_item_id', 'parent_id', 'COLUMN';
    PRINT '  ✓ Renamed: parent_workspace_item_id → parent_id';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 4 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 4 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 4 failed', 16, 1);
END CATCH
PRINT '';
GO

-- =============================================
-- STEP 5: RENAME item_id → entity_id, item_type → entity_type
-- =============================================
PRINT '========================================';
PRINT 'STEP 5: Renaming columns...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    -- Rename item_id → entity_id
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'item_id'
    )
    BEGIN
        EXEC sp_rename 'ws.workspace_items.item_id', 'entity_id', 'COLUMN';
        PRINT '  ✓ Renamed: item_id → entity_id';
    END
    ELSE
        PRINT '  ○ item_id already renamed';

    -- Rename item_type → entity_type
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'item_type'
    )
    BEGIN
        EXEC sp_rename 'ws.workspace_items.item_type', 'entity_type', 'COLUMN';
        PRINT '  ✓ Renamed: item_type → entity_type';
    END
    ELSE
        PRINT '  ○ item_type already renamed';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 5 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 5 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 5 failed', 16, 1);
END CATCH
PRINT '';
GO

-- =============================================
-- STEP 6: ADD SELF-REFERENCING FK CONSTRAINT
-- =============================================
PRINT '========================================';
PRINT 'STEP 6: Adding self-referencing FK...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    -- Add self-referencing FK (parent_id → workspace_items.id)
    ALTER TABLE ws.workspace_items
    ADD CONSTRAINT FK_workspace_items_parent
    FOREIGN KEY (parent_id) REFERENCES ws.workspace_items(id);

    PRINT '  ✓ Added FK: parent_id → workspace_items.id';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 6 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 6 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 6 failed', 16, 1);
END CATCH
PRINT '';
GO

-- =============================================
-- STEP 7: CREATE INDEXES FOR PERFORMANCE
-- =============================================
PRINT '========================================';
PRINT 'STEP 7: Creating indexes...';
PRINT '========================================';
PRINT '';

BEGIN TRANSACTION;
BEGIN TRY
    -- Index on parent_id for hierarchy queries
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'IX_workspace_items_parent_id'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_workspace_items_parent_id
        ON ws.workspace_items(parent_id)
        WHERE parent_id IS NOT NULL;
        PRINT '  ✓ Created index: IX_workspace_items_parent_id';
    END
    ELSE
        PRINT '  ○ Index IX_workspace_items_parent_id exists';

    -- Composite index for common queries
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('ws.workspace_items')
        AND name = 'IX_workspace_items_workspace_entity'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_workspace_items_workspace_entity
        ON ws.workspace_items(workspace_id, entity_type, entity_id);
        PRINT '  ✓ Created index: IX_workspace_items_workspace_entity';
    END
    ELSE
        PRINT '  ○ Index IX_workspace_items_workspace_entity exists';

    COMMIT TRANSACTION;
    PRINT '  ✅ Step 7 completed!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '  ❌ Step 7 failed: ' + ERROR_MESSAGE();
    RAISERROR('Step 7 failed', 16, 1);
END CATCH
PRINT '';
GO

-- =============================================
-- STEP 8: VERIFY MIGRATION
-- =============================================
PRINT '========================================';
PRINT 'STEP 8: Verifying migration...';
PRINT '========================================';
PRINT '';

-- Show schema
PRINT 'Current workspace_items schema:';
SELECT
    c.name as ColumnName,
    t.name as DataType,
    c.max_length as MaxLength,
    c.is_nullable as IsNullable,
    CASE WHEN pk.column_id IS NOT NULL THEN 'YES' ELSE 'NO' END as IsPrimaryKey,
    CASE WHEN fk.parent_column_id IS NOT NULL THEN 'YES' ELSE 'NO' END as IsForeignKey
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
LEFT JOIN (
    SELECT ic.object_id, ic.column_id
    FROM sys.index_columns ic
    INNER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.is_primary_key = 1
) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
LEFT JOIN sys.foreign_key_columns fk ON c.object_id = fk.parent_object_id AND c.column_id = fk.parent_column_id
WHERE c.object_id = OBJECT_ID('ws.workspace_items')
ORDER BY c.column_id;

PRINT '';

-- Show FK constraints
PRINT 'Foreign Key constraints:';
SELECT
    fk.name as ConstraintName,
    OBJECT_NAME(fk.parent_object_id) as TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) as ColumnName,
    OBJECT_NAME(fk.referenced_object_id) as ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) as ReferencedColumn
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('ws.workspace_items');

PRINT '';

-- Sample data verification
PRINT 'Sample workspace_items data (first 10 rows):';
SELECT TOP 10
    id,
    workspace_id,
    parent_id,
    entity_type,
    entity_id,
    created_at
FROM ws.workspace_items
ORDER BY id;

PRINT '';
PRINT '=============================================';
PRINT 'MIGRATION COMPLETED SUCCESSFULLY!';
PRINT 'Completed at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '=============================================';
GO
