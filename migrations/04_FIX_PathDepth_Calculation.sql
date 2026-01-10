-- =============================================
-- Migration: Fix PathDepth calculation for workspace_items
-- Description: Recalculate PathDepth based on PathIds
-- Author: Claude Code
-- Date: 2026-01-04
-- =============================================

PRINT 'Fixing PathDepth calculation in workspace_items...';
GO

-- Step 1: Show current incorrect values (if any)
PRINT 'Checking for incorrect PathDepth values...';
SELECT
    Id,
    WorkspaceId,
    PathIds,
    PathDepth AS CurrentPathDepth,
    (LEN(PathIds) - LEN(REPLACE(PathIds, '/', '')) - 1) AS CorrectPathDepth,
    CASE
        WHEN PathDepth = (LEN(PathIds) - LEN(REPLACE(PathIds, '/', '')) - 1) THEN 'OK'
        ELSE 'WRONG'
    END AS Status
FROM [ws].[workspace_items]
WHERE PathDepth != (LEN(PathIds) - LEN(REPLACE(PathIds, '/', '')) - 1);

DECLARE @incorrectCount INT;
SELECT @incorrectCount = COUNT(*)
FROM [ws].[workspace_items]
WHERE PathDepth != (LEN(PathIds) - LEN(REPLACE(PathIds, '/', '')) - 1);

PRINT '';
PRINT 'Found ' + CAST(@incorrectCount AS NVARCHAR) + ' items with incorrect PathDepth';
GO

-- Step 2: Fix PathDepth values
PRINT 'Updating PathDepth values...';

UPDATE [ws].[workspace_items]
SET PathDepth = (LEN(PathIds) - LEN(REPLACE(PathIds, '/', '')) - 1)
WHERE PathDepth != (LEN(PathIds) - LEN(REPLACE(PathIds, '/', '')) - 1);

PRINT 'Updated ' + CAST(@@ROWCOUNT AS NVARCHAR) + ' rows';
GO

-- Step 3: Verify fix
PRINT '';
PRINT 'Verifying PathDepth values...';
SELECT
    PathDepth,
    COUNT(*) AS ItemCount,
    MIN(PathIds) AS ExamplePathIds
FROM [ws].[workspace_items]
GROUP BY PathDepth
ORDER BY PathDepth;
GO

-- Step 4: Show examples
PRINT '';
PRINT 'Sample data after fix:';
SELECT TOP 10
    Id,
    WorkspaceId,
    ParentId,
    PathIds,
    PathDepth,
    EntityType
FROM [ws].[workspace_items]
ORDER BY PathDepth, Id;
GO

PRINT '';
PRINT 'Migration completed successfully!';
PRINT '';
PRINT 'PathDepth calculation formula:';
PRINT '  PathDepth = Count(''/'') - 1';
PRINT '';
PRINT 'NOTE: Workspace is considered depth=0 (root), folders start at depth=1';
PRINT '';
PRINT 'Examples:';
PRINT '  /175/          → 2 slashes → 2 - 1 = 1 (root folder - first level child of workspace)';
PRINT '  /175/174/      → 3 slashes → 3 - 1 = 2 (depth 2 - nested folder)';
PRINT '  /175/174/180/  → 4 slashes → 4 - 1 = 3 (depth 3 - nested folder)';
GO
