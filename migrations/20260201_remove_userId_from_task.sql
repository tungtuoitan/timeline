-- Migration: Remove user_id column from pro.task table
-- Date: 2026-02-01
-- Description: Tasks inherit user ownership through project relationship
--              (each project belongs to a single user, tasks belong to projects)

-- =====================================================
-- Step 1: Drop the index on user_id (if exists)
-- =====================================================
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_task_user_id'
    AND object_id = OBJECT_ID('pro.task')
)
BEGIN
    DROP INDEX IX_task_user_id ON pro.task;
    PRINT 'Dropped index IX_task_user_id from pro.task';
END
GO

-- =====================================================
-- Step 2: Drop foreign key constraint (if exists)
-- =====================================================
IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_task_user_id'
    AND parent_object_id = OBJECT_ID('pro.task')
)
BEGIN
    ALTER TABLE pro.task
    DROP CONSTRAINT FK_task_user_id;
    PRINT 'Dropped foreign key FK_task_user_id from pro.task';
END
GO

-- =====================================================
-- Step 3: Drop the user_id column
-- =====================================================
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'pro'
    AND TABLE_NAME = 'task'
    AND COLUMN_NAME = 'user_id'
)
BEGIN
    ALTER TABLE pro.task
    DROP COLUMN user_id;
    PRINT 'Dropped user_id column from pro.task';
END
GO

PRINT 'Migration completed: user_id removed from pro.task';
PRINT 'Tasks now inherit user ownership through their project (pro.project.user_id)';
