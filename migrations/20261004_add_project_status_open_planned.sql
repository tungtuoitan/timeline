-- =============================================
-- Project statuses: add 'open' and 'planned' (TungRoot issue 0095, step 7)
-- Flow: open -> planned -> active -> paused -> completed / dropped
-- 'open' was already used by project #10 but missing from the registry.
-- Idempotent: safe to re-run.
--
-- Rollback:
--   DELETE FROM dbo.standard_registries WHERE type = 'project_status' AND code IN ('open', 'planned');
-- =============================================

IF NOT EXISTS (SELECT 1 FROM dbo.standard_registries WHERE type = 'project_status' AND code = 'open')
BEGIN
    INSERT INTO dbo.standard_registries (code, description, type, is_active, created_date, created_by)
    VALUES ('open', 'Open', 'project_status', 1, SYSUTCDATETIME(), 'system');
    PRINT 'Added project_status open';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.standard_registries WHERE type = 'project_status' AND code = 'planned')
BEGIN
    INSERT INTO dbo.standard_registries (code, description, type, is_active, created_date, created_by)
    VALUES ('planned', 'Planned', 'project_status', 1, SYSUTCDATETIME(), 'system');
    PRINT 'Added project_status planned';
END
GO
