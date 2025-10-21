-- =============================================
-- Migration: Make parent_tag_id NULLABLE in workspace_items
-- Date: October 21, 2025
-- Purpose: Allow root-level items in workspace (parent_tag_id = NULL)
-- =============================================

USE [SuperApp-dev];
GO

-- Step 1: Drop the unique constraint first (will be recreated)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_workspace_items_unique')
BEGIN
    ALTER TABLE workspace_items DROP CONSTRAINT UQ_workspace_items_unique;
    PRINT 'Dropped constraint UQ_workspace_items_unique';
END
GO

-- Step 2: Drop the foreign key constraint
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_workspace_items_parent_tag')
BEGIN
    ALTER TABLE workspace_items DROP CONSTRAINT FK_workspace_items_parent_tag;
    PRINT 'Dropped constraint FK_workspace_items_parent_tag';
END
GO

-- Step 3: Alter the column to allow NULL
ALTER TABLE workspace_items 
ALTER COLUMN parent_tag_id INT NULL;
GO
PRINT 'Changed parent_tag_id to allow NULL';
GO

-- Step 4: Recreate the foreign key constraint (with NULL support)
ALTER TABLE workspace_items
ADD CONSTRAINT FK_workspace_items_parent_tag 
    FOREIGN KEY (parent_tag_id) 
    REFERENCES tags(tag_id);
GO
PRINT 'Recreated FK_workspace_items_parent_tag (allows NULL)';
GO

-- Step 5: Recreate the unique constraint
-- Note: This unique constraint allows multiple rows with NULL parent_tag_id
-- because SQL Server treats each NULL as distinct in unique constraints
ALTER TABLE workspace_items
ADD CONSTRAINT UQ_workspace_items_unique 
    UNIQUE (workspace_id, parent_tag_id, child_type, child_id);
GO
PRINT 'Recreated UQ_workspace_items_unique (allows NULL)';
GO

-- Step 6: Verify the changes
PRINT '';
PRINT 'Verification:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable,
    c.max_length AS MaxLength
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('workspace_items')
    AND c.name = 'parent_tag_id';
GO

PRINT '';
PRINT 'Migration completed successfully!';
PRINT 'parent_tag_id is now NULLABLE for root-level items.';
GO

-- =============================================
-- Usage Examples:
-- =============================================
-- Root-level item (parent_tag_id = NULL, depth = 0):
-- INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, depth, added_by)
-- VALUES (1, NULL, 'tag', 5, 0, 1);

-- Nested item (parent_tag_id specified, depth > 0):
-- INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, depth, added_by)
-- VALUES (1, 5, 'tag', 6, 1, 1);
-- =============================================
