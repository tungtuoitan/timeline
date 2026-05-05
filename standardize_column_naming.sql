-- ============================================================
-- Standardize DB column/table names to snake_case
-- Run once on dev + production DB
-- ============================================================

-- 1. k.node — PathIds, PathDepth
EXEC sp_rename 'k.node.PathIds',    'path_ids',    'COLUMN';
EXEC sp_rename 'k.node.PathDepth',  'path_depth',  'COLUMN';

-- 2. pro.TargetKeywords — rename table + all columns
EXEC sp_rename 'pro.TargetKeywords', 'target_keywords';
EXEC sp_rename 'pro.target_keywords.Id',         'id',          'COLUMN';
EXEC sp_rename 'pro.target_keywords.TargetId',   'target_id',   'COLUMN';
EXEC sp_rename 'pro.target_keywords.TargetType', 'target_type', 'COLUMN';
EXEC sp_rename 'pro.target_keywords.KeywordId',  'keyword_id',  'COLUMN';

-- 3. pro.TaskWorkspaceItem — rename table + all columns
EXEC sp_rename 'pro.TaskWorkspaceItem', 'task_workspace_item';
EXEC sp_rename 'pro.task_workspace_item.Id',              'id',                 'COLUMN';
EXEC sp_rename 'pro.task_workspace_item.TaskId',          'task_id',            'COLUMN';
EXEC sp_rename 'pro.task_workspace_item.WorkspaceItemId', 'workspace_item_id',  'COLUMN';
EXEC sp_rename 'pro.task_workspace_item.ItemType',        'item_type',          'COLUMN';

-- 4. pro.task — FolderWorkspaceItemId, status → status_code
EXEC sp_rename 'pro.task.FolderWorkspaceItemId', 'folder_workspace_item_id', 'COLUMN';
EXEC sp_rename 'pro.task.status',                'status_code',              'COLUMN';

-- 5. pro.project — status → status_code
EXEC sp_rename 'pro.project.status', 'status_code', 'COLUMN';

-- 6. urm.user_profiles — add deleted_at
ALTER TABLE urm.user_profiles ADD deleted_at DATETIME2 NULL;
