-- =============================================
-- Migration: Update standard_registries table structure (FIXED)
-- Description:
--   - Drop all constraints on type_code column first
--   - Drop old type_code column
--   - Add new columns: code, type, json_detail, created_by, last_modified_by, last_modified_date
--   - Rename created_at to created_date
-- =============================================

-- IMPORTANT: Replace 'SuperApp' with your actual database name
USE [SuperApp];
GO

PRINT '=== Starting migration for standard_registries table ===';
GO

-- =============================================
-- Step 1: Drop all constraints on type_code column
-- =============================================
DECLARE @ConstraintName NVARCHAR(200);
DECLARE @SQL NVARCHAR(MAX);

-- Find and drop all constraints on type_code column
DECLARE constraint_cursor CURSOR FOR
SELECT
    OBJECT_NAME(fk.constraint_object_id) AS constraint_name
FROM
    sys.foreign_key_columns AS fk
    INNER JOIN sys.columns AS c ON fk.parent_column_id = c.column_id AND fk.parent_object_id = c.object_id
WHERE
    c.object_id = OBJECT_ID('dbo.standard_registries')
    AND c.name = 'type_code'
UNION
SELECT
    i.name AS constraint_name
FROM
    sys.indexes AS i
    INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    INNER JOIN sys.columns AS c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE
    c.object_id = OBJECT_ID('dbo.standard_registries')
    AND c.name = 'type_code'
    AND i.is_primary_key = 0
    AND i.is_unique_constraint = 1;

OPEN constraint_cursor;
FETCH NEXT FROM constraint_cursor INTO @ConstraintName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SQL = 'ALTER TABLE [dbo].[standard_registries] DROP CONSTRAINT [' + @ConstraintName + ']';
    PRINT 'Dropping constraint: ' + @ConstraintName;
    EXEC sp_executesql @SQL;
    FETCH NEXT FROM constraint_cursor INTO @ConstraintName;
END

CLOSE constraint_cursor;
DEALLOCATE constraint_cursor;
GO

-- =============================================
-- Step 2: Drop indexes on type_code column
-- =============================================
DECLARE @IndexName NVARCHAR(200);
DECLARE @SQL NVARCHAR(MAX);

DECLARE index_cursor CURSOR FOR
SELECT
    i.name AS index_name
FROM
    sys.indexes AS i
    INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    INNER JOIN sys.columns AS c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE
    c.object_id = OBJECT_ID('dbo.standard_registries')
    AND c.name = 'type_code'
    AND i.is_primary_key = 0
    AND i.is_unique_constraint = 0;

OPEN index_cursor;
FETCH NEXT FROM index_cursor INTO @IndexName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SQL = 'DROP INDEX [' + @IndexName + '] ON [dbo].[standard_registries]';
    PRINT 'Dropping index: ' + @IndexName;
    EXEC sp_executesql @SQL;
    FETCH NEXT FROM index_cursor INTO @IndexName;
END

CLOSE index_cursor;
DEALLOCATE index_cursor;
GO

-- =============================================
-- Step 3: Drop old columns
-- =============================================

-- Drop type_code column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'type_code')
BEGIN
    ALTER TABLE [dbo].[standard_registries] DROP COLUMN [type_code];
    PRINT 'Dropped column: type_code';
END
ELSE
BEGIN
    PRINT 'Column type_code does not exist (already dropped)';
END
GO

-- Drop updated_at column (we'll use last_modified_date instead)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'updated_at')
BEGIN
    ALTER TABLE [dbo].[standard_registries] DROP COLUMN [updated_at];
    PRINT 'Dropped column: updated_at';
END
GO

-- =============================================
-- Step 4: Add new columns
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'code')
BEGIN
    ALTER TABLE [dbo].[standard_registries] ADD [code] NVARCHAR(100) NOT NULL DEFAULT '';
    PRINT 'Added column: code';
END
ELSE
BEGIN
    PRINT 'Column code already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'type')
BEGIN
    ALTER TABLE [dbo].[standard_registries] ADD [type] NVARCHAR(100) NOT NULL DEFAULT '';
    PRINT 'Added column: type';
END
ELSE
BEGIN
    PRINT 'Column type already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'json_detail')
BEGIN
    ALTER TABLE [dbo].[standard_registries] ADD [json_detail] NVARCHAR(MAX) NULL;
    PRINT 'Added column: json_detail';
END
ELSE
BEGIN
    PRINT 'Column json_detail already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'created_by')
BEGIN
    ALTER TABLE [dbo].[standard_registries] ADD [created_by] NVARCHAR(255) NULL;
    PRINT 'Added column: created_by';
END
ELSE
BEGIN
    PRINT 'Column created_by already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'last_modified_by')
BEGIN
    ALTER TABLE [dbo].[standard_registries] ADD [last_modified_by] NVARCHAR(255) NULL;
    PRINT 'Added column: last_modified_by';
END
ELSE
BEGIN
    PRINT 'Column last_modified_by already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'last_modified_date')
BEGIN
    ALTER TABLE [dbo].[standard_registries] ADD [last_modified_date] DATETIME NULL;
    PRINT 'Added column: last_modified_date';
END
ELSE
BEGIN
    PRINT 'Column last_modified_date already exists';
END
GO

-- =============================================
-- Step 5: Rename columns
-- =============================================

-- Rename created_at to created_date
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.standard_registries') AND name = 'created_at')
BEGIN
    EXEC sp_rename 'dbo.standard_registries.created_at', 'created_date', 'COLUMN';
    PRINT 'Renamed column: created_at -> created_date';
END
ELSE
BEGIN
    PRINT 'Column created_at does not exist (already renamed)';
END
GO

-- =============================================
-- Step 6: Create new indexes
-- =============================================

-- Create index on code + type combination for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_standard_registries_type_code' AND object_id = OBJECT_ID('dbo.standard_registries'))
BEGIN
    CREATE INDEX [IX_standard_registries_type_code] ON [dbo].[standard_registries] ([type], [code]);
    PRINT 'Created index: IX_standard_registries_type_code';
END
ELSE
BEGIN
    PRINT 'Index IX_standard_registries_type_code already exists';
END
GO

-- Create index on type for filtering by type
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_standard_registries_type' AND object_id = OBJECT_ID('dbo.standard_registries'))
BEGIN
    CREATE INDEX [IX_standard_registries_type] ON [dbo].[standard_registries] ([type]);
    PRINT 'Created index: IX_standard_registries_type';
END
ELSE
BEGIN
    PRINT 'Index IX_standard_registries_type already exists';
END
GO

PRINT '=== Migration completed successfully! ===';
GO
