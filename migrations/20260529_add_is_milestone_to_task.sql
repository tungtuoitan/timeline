/*
  Migration: Add is_milestone column to pro.task
  Date: 2026-05-29

  Adds a boolean is_milestone flag (NOT NULL, default 0) so individual tasks
  can be marked as milestones (key dates / diamond markers) in the UI.

  Apply order: dev (SuperApp-dev) → prod (SuperApp-pro). Run while idle.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'pro.task')
      AND [name] = N'is_milestone'
)
BEGIN
    ALTER TABLE pro.task
        ADD is_milestone BIT NOT NULL CONSTRAINT DF_pro_task_is_milestone DEFAULT (0);
END
GO
