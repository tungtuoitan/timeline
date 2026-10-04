-- =============================================
-- Follow-up of 20261004_time_utc_dateonly (TungRoot task #1450): shift the remaining
-- Vietnam-time instants to UTC.
--
-- VietnamDateTime.Now() (UTC+7 labelled as UTC) was used from commit fdd20d2
-- (2026-05-04 23:40 +07 = 16:40 UTC) until the UTC deploy (2026-10-04 13:06 UTC) not only
-- for pro/log/daily_log (already fixed) but also for K, workspace, notes, files, flow,
-- user_profiles. Before fdd20d2 these tables were written with DateTime.UtcNow.
--
-- Rule: a value whose label is in [2026-05-04 16:40, 2026-10-04 13:06) can only have been
-- written in Vietnam time -> -7h. Labels in [2026-10-04 13:06, 20:06) are ambiguous (VN just
-- before deploy or UTC just after); the only VN ones (verified by hand) are fixed by id:
-- ws.workspaces 96..113 created_at (0095 migration, 16:21 label) and standard_registries
-- 42, 43 created_date (SYSDATETIME in 20261004_add_project_status_open_planned).
-- dbo.Keywords is NOT touched: its window rows look UTC (other writers) and the table is
-- scheduled for removal (#1449).
-- Idempotent: guarded by dbo.schema_migrations.
-- =============================================
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE name = '20261004_time_utc_fix_vn_window')
BEGIN
    BEGIN TRAN;

    DECLARE @from DATETIME2 = '2026-05-04T16:40:00', @to DATETIME2 = '2026-10-04T13:06:00';
    DECLARE @shift TABLE (tbl NVARCHAR(200), col NVARCHAR(100));
    INSERT @shift VALUES
        ('k.point_history','created_at'),
        ('k.question_status_history','changed_at'),
        ('k.node_status_history','changed_at'),
        ('k.question','created_at'),('k.question','updated_at'),('k.question','deleted_at'),
        ('k.node','created_at'),('k.node','updated_at'),('k.node','deleted_at'),
        ('k.knowledge','created_at'),('k.knowledge','updated_at'),('k.knowledge','deleted_at'),
        ('dbo.notes','created_at'),('dbo.notes','updated_at'),('dbo.notes','deleted_at'),
        ('dbo.files','created_at'),('dbo.files','updated_at'),('dbo.files','deleted_at'),
        ('ws.workspaces','created_at'),('ws.workspaces','updated_at'),('ws.workspaces','deleted_at'),
        ('ws.folders','created_at'),('ws.folders','updated_at'),('ws.folders','deleted_at'),
        ('ws.workspace_items','created_at'),('ws.workspace_items','updated_at'),('ws.workspace_items','deleted_at'),
        ('pro.flow_edge','created_at'),('pro.flow_edge','updated_at'),('pro.flow_edge','deleted_at'),
        ('pro.flow_node_position','created_at'),('pro.flow_node_position','updated_at'),
        ('urm.user_profiles','created_at'),('urm.user_profiles','updated_at'),
        ('dbo.standard_registries','last_modified_date');

    DECLARE @t NVARCHAR(200), @c NVARCHAR(100), @q NVARCHAR(MAX), @n INT;
    DECLARE sh CURSOR LOCAL FAST_FORWARD FOR SELECT tbl, col FROM @shift;
    OPEN sh; FETCH NEXT FROM sh INTO @t, @c;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF COL_LENGTH(@t, @c) IS NOT NULL
        BEGIN
            SET @q = N'UPDATE ' + @t + N' SET ' + QUOTENAME(@c) + N' = DATEADD(hour, -7, ' + QUOTENAME(@c) + N')'
                   + N' WHERE ' + QUOTENAME(@c) + N' >= @from AND ' + QUOTENAME(@c) + N' < @to; SET @n = @@ROWCOUNT;';
            EXEC sp_executesql @q, N'@from DATETIME2, @to DATETIME2, @n INT OUTPUT', @from, @to, @n OUTPUT;
            PRINT CONCAT(@t, '.', @c, ': ', @n);
        END
        FETCH NEXT FROM sh INTO @t, @c;
    END
    CLOSE sh; DEALLOCATE sh;

    -- ambiguous zone, verified VN rows (ids checked on prod only)
    IF DB_NAME() = 'SuperApp-pro'
    BEGIN
    UPDATE ws.workspaces SET created_at = DATEADD(hour, -7, created_at)
        WHERE id BETWEEN 96 AND 113 AND created_at >= @to AND created_at < '2026-10-04T20:06:00';
    PRINT CONCAT('ws.workspaces.created_at (ids 96..113): ', @@ROWCOUNT);
    UPDATE dbo.standard_registries SET created_date = DATEADD(hour, -7, created_date)
        WHERE id IN (42, 43) AND created_date >= @to AND created_date < '2026-10-04T20:06:00';
    PRINT CONCAT('dbo.standard_registries.created_date (ids 42, 43): ', @@ROWCOUNT);
    END

    INSERT dbo.schema_migrations (name) VALUES ('20261004_time_utc_fix_vn_window');
    COMMIT;
    PRINT 'VN window -> UTC shift applied';
END
ELSE
    PRINT 'skip: 20261004_time_utc_fix_vn_window already applied';
GO
