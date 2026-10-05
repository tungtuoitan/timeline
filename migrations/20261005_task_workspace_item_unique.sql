-- =============================================
-- pro.task_workspace_item: one row per (task, workspace item) — task #1477 (task/project links)
--   The table already exists (FKs to pro.task and ws.workspace_items, both ON DELETE CASCADE);
--   LinkService reads/writes it again after the old endpoints were removed.
--   Removes duplicate rows (keeps the lowest id) before adding the unique index.
-- Idempotent: safe to re-run.
--
-- Rollback:
--   DROP INDEX IF EXISTS ux_task_workspace_item_task_item ON pro.task_workspace_item;
-- =============================================

;WITH d AS (
    SELECT id, ROW_NUMBER() OVER (PARTITION BY task_id, workspace_item_id ORDER BY id) AS rn
    FROM pro.task_workspace_item
)
DELETE FROM d WHERE rn > 1;
PRINT CONCAT('Removed duplicate pro.task_workspace_item rows: ', @@ROWCOUNT);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ux_task_workspace_item_task_item' AND object_id = OBJECT_ID('pro.task_workspace_item'))
BEGIN
    CREATE UNIQUE INDEX ux_task_workspace_item_task_item ON pro.task_workspace_item (task_id, workspace_item_id);
    PRINT 'Created ux_task_workspace_item_task_item';
END
GO
