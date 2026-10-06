-- =============================================
-- Project statuses: open > in_progress / paused > completed / dropped (SuperApp task #1497)
-- 'active' trùng nghĩa với 'open' -> đổi thành 'in_progress'; 'planned' (thêm nhầm ở
-- 20261004_add_project_status_open_planned) bỏ, project đang planned -> in_progress.
-- Saved project grid filters -> 'open,in_progress' (default mới).
-- Idempotent: safe to re-run.
--
-- Rollback:
--   UPDATE dbo.standard_registries SET code = 'active', description = 'Active'
--       WHERE type = 'project_status' AND code = 'in_progress';
--   INSERT INTO dbo.standard_registries (code, description, type, is_active, created_date, created_by)
--       VALUES ('planned', 'Planned', 'project_status', 1, SYSUTCDATETIME(), 'system');
--   UPDATE pro.project SET status_code = 'active' WHERE status_code = 'in_progress';
--   (project từng 'planned' + filter cũ không khôi phục tự động được)
-- =============================================

IF EXISTS (SELECT 1 FROM dbo.standard_registries WHERE type = 'project_status' AND code = 'active')
   AND NOT EXISTS (SELECT 1 FROM dbo.standard_registries WHERE type = 'project_status' AND code = 'in_progress')
BEGIN
    UPDATE dbo.standard_registries
    SET code = 'in_progress', description = 'In Progress',
        last_modified_date = SYSUTCDATETIME(), last_modified_by = 'system'
    WHERE type = 'project_status' AND code = 'active';
    PRINT 'Renamed project_status active -> in_progress';
END
GO

DELETE FROM dbo.standard_registries WHERE type = 'project_status' AND code = 'planned';
PRINT CONCAT('Removed project_status planned: ', @@ROWCOUNT);
GO

UPDATE pro.project SET status_code = 'in_progress' WHERE status_code IN ('active', 'planned');
PRINT CONCAT('Projects -> in_progress: ', @@ROWCOUNT);
GO

UPDATE urm.user_profiles
SET filters = JSON_MODIFY(filters, '$.projectGrid.statusCode', 'open,in_progress')
WHERE ISJSON(filters) = 1
  AND JSON_VALUE(filters, '$.projectGrid.statusCode') IS NOT NULL
  AND JSON_VALUE(filters, '$.projectGrid.statusCode') <> 'open,in_progress';
PRINT CONCAT('Saved project grid filters reset: ', @@ROWCOUNT);
GO
