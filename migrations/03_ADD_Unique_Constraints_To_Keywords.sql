-- =============================================
-- Migration: Add UNIQUE constraints to Keywords table
-- Description: Ensure TargetItemId is unique (1 workspace_items = 1 keyword)
-- Author: Claude Code
-- Date: 2026-01-04
-- =============================================

PRINT 'Adding UNIQUE constraints to Keywords table...';
GO

-- Step 1: Check for duplicate TargetItemId entries
PRINT 'Checking for duplicate TargetItemId entries...';
GO

DECLARE @duplicateCount INT;
SELECT @duplicateCount = COUNT(*)
FROM (
    SELECT TargetItemId, COUNT(*) as cnt
    FROM [dbo].[Keywords]
    WHERE TargetItemId IS NOT NULL
    GROUP BY TargetItemId
    HAVING COUNT(*) > 1
) AS duplicates;

IF @duplicateCount > 0
BEGIN
    PRINT 'WARNING: Found duplicate TargetItemId entries!';
    PRINT 'Listing duplicates:';

    SELECT
        k.TargetItemId,
        COUNT(*) as DuplicateCount,
        STRING_AGG(CAST(k.Id AS NVARCHAR), ', ') as KeywordIds
    FROM [dbo].[Keywords] k
    WHERE k.TargetItemId IS NOT NULL
    GROUP BY k.TargetItemId
    HAVING COUNT(*) > 1;

    PRINT '';
    PRINT 'ACTION REQUIRED: Please resolve duplicates before adding UNIQUE constraint!';
    PRINT 'Suggested fix:';
    PRINT '  1. Identify which keywords to keep (usually the one with older CreatedAt)';
    PRINT '  2. Delete duplicate keywords manually';
    PRINT '  3. Re-run this migration';
    PRINT '';

    -- Uncomment to auto-delete duplicates (keeps oldest by CreatedAt)
    -- DELETE k FROM [dbo].[Keywords] k
    -- INNER JOIN (
    --     SELECT TargetItemId, MIN(CreatedAt) as OldestCreatedAt
    --     FROM [dbo].[Keywords]
    --     WHERE TargetItemId IS NOT NULL
    --     GROUP BY TargetItemId
    --     HAVING COUNT(*) > 1
    -- ) duplicates ON k.TargetItemId = duplicates.TargetItemId
    -- WHERE k.CreatedAt > duplicates.OldestCreatedAt;

    RAISERROR('Migration aborted: Duplicate TargetItemId entries found', 16, 1);
END
ELSE
BEGIN
    PRINT 'No duplicate TargetItemId entries found. Proceeding...';
END
GO

-- Step 2: Drop existing non-unique index if exists
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Keywords_TargetItemId' AND object_id = OBJECT_ID('[dbo].[Keywords]'))
BEGIN
    DROP INDEX [IX_Keywords_TargetItemId] ON [dbo].[Keywords];
    PRINT 'Dropped existing IX_Keywords_TargetItemId index';
END
GO

-- Step 3: Create UNIQUE index on TargetItemId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Keywords_TargetItemId' AND object_id = OBJECT_ID('[dbo].[Keywords]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Keywords_TargetItemId]
        ON [dbo].[Keywords] ([TargetItemId])
        WHERE [TargetItemId] IS NOT NULL;

    PRINT 'Created UNIQUE index UQ_Keywords_TargetItemId';
END
ELSE
BEGIN
    PRINT 'UNIQUE index UQ_Keywords_TargetItemId already exists';
END
GO

-- Step 4: Verify constraints
PRINT '';
PRINT 'Verifying UNIQUE constraints:';
SELECT
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('[dbo].[Keywords]')
    AND i.name IN ('UQ_Keywords_Link', 'UQ_Keywords_Name_NameIndex', 'UQ_Keywords_TargetItemId')
ORDER BY i.name;
GO

PRINT '';
PRINT 'Migration completed successfully!';
PRINT 'Keywords table now has the following UNIQUE constraints:';
PRINT '  1. UQ_Keywords_Link - Each Link is unique';
PRINT '  2. UQ_Keywords_Name_NameIndex - Each Name+NameIndex combination is unique';
PRINT '  3. UQ_Keywords_TargetItemId - Each TargetItemId is unique (prevents duplicate keywords for same workspace_items)';
GO
