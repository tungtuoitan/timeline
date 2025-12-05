-- =============================================
-- Migration: Rename folder_id to parent_id in workspace_items table
-- Description: Rename column for better clarity - parent_id instead of folder_id
-- Date: 2025-12-01
-- =============================================

USE [SuperApp-dev];
GO

-- Step 1: Drop existing foreign key constraint
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_workspace_items_parent_folder')
BEGIN
    ALTER TABLE [ws].[workspace_items] DROP CONSTRAINT [fk_workspace_items_parent_folder];
    PRINT 'Dropped FK constraint: fk_workspace_items_parent_folder';
END
GO

-- Step 2: Drop existing index
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_workspace_items_folder_id' AND object_id = OBJECT_ID('ws.workspace_items'))
BEGIN
    DROP INDEX [idx_workspace_items_folder_id] ON [ws].[workspace_items];
    PRINT 'Dropped index: idx_workspace_items_folder_id';
END
GO

-- Step 3: Rename column using sp_rename
EXEC sp_rename 'ws.workspace_items.folder_id', 'parent_id', 'COLUMN';
PRINT 'Renamed column: folder_id -> parent_id';
GO

-- Step 4: Recreate foreign key constraint with new column name
ALTER TABLE [ws].[workspace_items]
    ADD CONSTRAINT [fk_workspace_items_parent_folder]
    FOREIGN KEY ([parent_id]) REFERENCES [ws].[folders]([id])
    ON DELETE NO ACTION;
PRINT 'Recreated FK constraint with parent_id';
GO

-- Step 5: Recreate index with new column name
CREATE NONCLUSTERED INDEX [idx_workspace_items_parent_id]
    ON [ws].[workspace_items]([parent_id])
    INCLUDE ([workspace_id], [item_type], [item_id]);
PRINT 'Recreated index: idx_workspace_items_parent_id';
GO

-- Step 6: Verify the change
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'ws'
    AND TABLE_NAME = 'workspace_items'
    AND COLUMN_NAME = 'parent_id';
GO

PRINT 'Migration completed successfully!';
PRINT 'Column renamed: folder_id -> parent_id';
GO
