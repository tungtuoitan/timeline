-- Migration: Refactor dbo.Keywords table
-- 1. Copy WorkspaceId → TargetItemId for workspace keywords (before dropping column)
-- 2. Drop WorkspaceId, PathIds, ExternalUrl columns
-- 3. Add sa/ prefix to existing workspace/folder/note/file links

-- Step 1: Copy WorkspaceId → TargetItemId for workspace keywords
UPDATE dbo.Keywords
SET TargetItemId = WorkspaceId
WHERE [Type] = 'workspace'
  AND WorkspaceId IS NOT NULL
  AND (TargetItemId IS NULL OR TargetItemId = 0);

-- Step 2: Drop FK constraint on WorkspaceId (find by dynamic lookup)
DECLARE @fkName NVARCHAR(200);
SELECT @fkName = fk.name
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Keywords')
  AND c.name = 'WorkspaceId';

IF @fkName IS NOT NULL
    EXEC('ALTER TABLE dbo.Keywords DROP CONSTRAINT [' + @fkName + ']');

-- Step 3: Drop columns
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Keywords') AND name = 'WorkspaceId')
    ALTER TABLE dbo.Keywords DROP COLUMN WorkspaceId;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Keywords') AND name = 'PathIds')
    ALTER TABLE dbo.Keywords DROP COLUMN PathIds;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Keywords') AND name = 'ExternalUrl')
    ALTER TABLE dbo.Keywords DROP COLUMN ExternalUrl;

-- Step 4: Add sa/ prefix to workspace/folder/note/file links (skip http/external and already-prefixed)
UPDATE dbo.Keywords
SET Link = 'sa/' + Link
WHERE [Type] IN ('workspace', 'folder', 'note', 'file')
  AND Link NOT LIKE 'sa/%'
  AND Link NOT LIKE 'http://%'
  AND Link NOT LIKE 'https://%'
  AND Link <> '';
