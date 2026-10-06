-- =============================================
-- Project status 'background_progress' (SuperApp task #1497) — project chạy lâu dài
-- (tài chính, begger gang...). Cùng code/label với task_status background_progress.
-- Flow: open > in_progress / background_progress / paused > completed / dropped
-- Saved project grid filters 'open,in_progress' -> thêm background_progress (default mới).
-- Idempotent: safe to re-run.
--
-- Rollback:
--   UPDATE pro.project SET status_code = 'in_progress' WHERE status_code = 'background_progress';
--   DELETE FROM dbo.standard_registries WHERE type = 'project_status' AND code = 'background_progress';
-- =============================================

IF NOT EXISTS (SELECT 1 FROM dbo.standard_registries WHERE type = 'project_status' AND code = 'background_progress')
BEGIN
    INSERT INTO dbo.standard_registries (code, description, type, is_active, created_date, created_by)
    VALUES ('background_progress', 'Bg Progress', 'project_status', 1, SYSUTCDATETIME(), 'system');
    PRINT 'Added project_status background_progress';
END
GO

UPDATE urm.user_profiles
SET filters = JSON_MODIFY(filters, '$.projectGrid.statusCode', 'open,in_progress,background_progress')
WHERE ISJSON(filters) = 1
  AND JSON_VALUE(filters, '$.projectGrid.statusCode') = 'open,in_progress';
PRINT CONCAT('Saved project grid filters + background_progress: ', @@ROWCOUNT);
GO
