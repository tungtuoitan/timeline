-- Migration: Remove copy_info columns
-- copy_info was intended for a future copy/clone feature that will not be implemented.

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ws.workspace_items') AND name = 'copy_info')
    ALTER TABLE ws.workspace_items DROP COLUMN copy_info;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ws.folders') AND name = 'copy_info')
    ALTER TABLE ws.folders DROP COLUMN copy_info;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.notes') AND name = 'copy_info')
    ALTER TABLE dbo.notes DROP COLUMN copy_info;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.files') AND name = 'copy_info')
    ALTER TABLE dbo.files DROP COLUMN copy_info;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ws.workspaces') AND name = 'copy_info')
    ALTER TABLE ws.workspaces DROP COLUMN copy_info;
