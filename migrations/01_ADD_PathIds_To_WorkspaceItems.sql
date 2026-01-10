-- =============================================
-- Migration: Add PathIds to workspace_items
-- Description: Add Materialized Path columns
-- Author: Claude Code
-- Date: 2026-01-04
-- =============================================

-- Step 1: Add new columns
ALTER TABLE [ws].[workspace_items]
ADD [PathIds] NVARCHAR(1000) NOT NULL,
    [PathDepth] INT NOT NULL,
    [Slug] NVARCHAR(255) NOT NULL;

PRINT 'Added PathIds, PathDepth, Slug columns';
GO

-- Step 2: Build PathIds for existing data using recursive CTE
--;WITH ItemHierarchy AS (
--    -- Anchor: Root items (ParentId IS NULL)
--    SELECT
--        Id,
--        ParentId,
--        CAST('/' + CAST(Id AS NVARCHAR(10)) + '/' AS NVARCHAR(1000)) AS PathIds,
--        0 AS PathDepth
--    FROM [ws].[workspace_items]
--    WHERE ParentId IS NULL

--    UNION ALL

--    -- Recursive: Children
--    SELECT
--        wi.Id,
--        wi.ParentId,
--        CAST(ih.PathIds + CAST(wi.Id AS NVARCHAR(10)) + '/' AS NVARCHAR(1000)),
--        ih.PathDepth + 1
--    FROM [ws].[workspace_items] wi
--    INNER JOIN ItemHierarchy ih ON wi.ParentId = ih.Id
--)
--UPDATE wi
--SET
--    wi.PathIds = ih.PathIds,
--    wi.PathDepth = ih.PathDepth,
--    wi.Slug = LOWER(REPLACE(REPLACE(REPLACE(
--        -- Simple slug generation (can be improved)
--        CASE
--            WHEN wi.EntityType = 1 THEN w.Name
--            WHEN wi.EntityType = 2 THEN f.Name
--            WHEN wi.EntityType = 3 THEN n.Name
--            ELSE 'item'
--        END
--    , ' ', '-'), '/', '-'), '''', ''))
--FROM [ws].[workspace_items] wi
--INNER JOIN ItemHierarchy ih ON wi.Id = ih.Id
--LEFT JOIN [ws].[workspaces] w ON wi.EntityType = 1 AND wi.EntityId = w.Id
--LEFT JOIN [ws].[folders] f ON wi.EntityType = 2 AND wi.EntityId = f.Id
--LEFT JOIN [ws].[notes] n ON wi.EntityType = 3 AND wi.EntityId = n.Id;

--PRINT 'Built PathIds for existing records';
--GO



ALTER TABLE [ws].[workspace_items]
ADD CONSTRAINT [DF_WorkspaceItems_PathIds] DEFAULT '/' FOR [PathIds];

ALTER TABLE [ws].[workspace_items]
ADD CONSTRAINT [DF_WorkspaceItems_PathDepth] DEFAULT 0 FOR [PathDepth];

PRINT 'Made columns NOT NULL with defaults';
GO

-- Step 4: Create indexes
CREATE NONCLUSTERED INDEX [IX_WorkspaceItems_PathIds]
ON [ws].[workspace_items] ([PathIds]);

CREATE NONCLUSTERED INDEX [IX_WorkspaceItems_PathDepth]
ON [ws].[workspace_items] ([PathDepth]);

CREATE NONCLUSTERED INDEX [IX_WorkspaceItems_Slug]
ON [ws].[workspace_items] ([Slug]);

PRINT 'Created indexes on PathIds, PathDepth, Slug';
GO

-- Step 5: Add constraint for max depth
ALTER TABLE [ws].[workspace_items]
ADD CONSTRAINT [CK_WorkspaceItems_MaxDepth] CHECK ([PathDepth] <= 10);

PRINT 'Added max depth constraint (10 levels)';
GO

PRINT 'Migration completed successfully!';


