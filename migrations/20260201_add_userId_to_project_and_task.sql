-- Migration: Add user_id column to pro.project and pro.task tables
-- Date: 2026-02-01
-- Description: Add user_id for data isolation between users

-- =====================================================
-- Step 1: Add user_id column to pro.project
-- =====================================================
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'pro'
    AND TABLE_NAME = 'project'
    AND COLUMN_NAME = 'user_id'
)
BEGIN
    -- Add column as nullable first
    ALTER TABLE pro.project
    ADD user_id INT NULL;

    PRINT 'Added user_id column to pro.project';
END
GO

-- =====================================================
-- Step 2: Add user_id column to pro.task
-- =====================================================
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'pro'
    AND TABLE_NAME = 'task'
    AND COLUMN_NAME = 'user_id'
)
BEGIN
    -- Add column as nullable first
    ALTER TABLE pro.task
    ADD user_id INT NULL;

    PRINT 'Added user_id column to pro.task';
END
GO

-- =====================================================
-- Step 3: Update existing data with a default user_id
-- NOTE: Update this with actual user ID from your system
-- You may want to set this based on your business logic
-- =====================================================
-- Example: Set all existing projects to user_id = 1 (first user)
-- UPDATE pro.project SET user_id = 1 WHERE user_id IS NULL;
-- UPDATE pro.task SET user_id = 1 WHERE user_id IS NULL;

-- =====================================================
-- Step 4: Make columns NOT NULL after data is populated
-- Run this AFTER you have populated user_id for existing data
-- =====================================================
-- ALTER TABLE pro.project ALTER COLUMN user_id INT NOT NULL;
-- ALTER TABLE pro.task ALTER COLUMN user_id INT NOT NULL;

-- =====================================================
-- Step 5: Add indexes for faster queries
-- =====================================================
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_project_user_id'
    AND object_id = OBJECT_ID('pro.project')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_project_user_id
    ON pro.project (user_id);

    PRINT 'Created index IX_project_user_id on pro.project';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_task_user_id'
    AND object_id = OBJECT_ID('pro.task')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_task_user_id
    ON pro.task (user_id);

    PRINT 'Created index IX_task_user_id on pro.task';
END
GO

-- =====================================================
-- Step 6 (Optional): Add foreign key to dbo.users table
-- Uncomment if you want to enforce referential integrity
-- =====================================================
-- IF NOT EXISTS (
--     SELECT 1
--     FROM sys.foreign_keys
--     WHERE name = 'FK_project_user_id'
--     AND parent_object_id = OBJECT_ID('pro.project')
-- )
-- BEGIN
--     ALTER TABLE pro.project
--     ADD CONSTRAINT FK_project_user_id
--     FOREIGN KEY (user_id) REFERENCES dbo.users(id);
--
--     PRINT 'Added foreign key FK_project_user_id';
-- END
-- GO
--
-- IF NOT EXISTS (
--     SELECT 1
--     FROM sys.foreign_keys
--     WHERE name = 'FK_task_user_id'
--     AND parent_object_id = OBJECT_ID('pro.task')
-- )
-- BEGIN
--     ALTER TABLE pro.task
--     ADD CONSTRAINT FK_task_user_id
--     FOREIGN KEY (user_id) REFERENCES dbo.users(id);
--
--     PRINT 'Added foreign key FK_task_user_id';
-- END
-- GO

PRINT 'Migration completed: user_id added to pro.project and pro.task';
