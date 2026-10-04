-- =============================================
-- Time handling best practice (TungRoot task #1450 / old 0111): drop "Fake UTC".
--   * Calendar dates -> DATE: pro.task / pro.project start_date, end_date
--     (values at 17:00 are the old toISOString bug = 00:00 VN of the NEXT day -> fixed)
--   * Instants -> UTC: tables written in Vietnam time (VietnamDateTime / SYSDATETIME on a
--     +07 server) are shifted -7h ONCE. Tables already written in UTC (K, keywords, wiki,
--     workspace models, auth) are left as is; mixed columns (e.g. k.question.updated_at)
--     are not touched — up to 7h drift in history accepted (Tung, 2026-10-04).
--   * Column defaults SYSDATETIME()/GETDATE() -> SYSUTCDATETIME()
--   * urm.user_profiles.timezone (IANA id, default Asia/Ho_Chi_Minh) for API display
-- Idempotent: the one-shot data shift is guarded by dbo.schema_migrations.
-- =============================================
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID('dbo.schema_migrations') IS NULL
    CREATE TABLE dbo.schema_migrations (name NVARCHAR(200) NOT NULL PRIMARY KEY, applied_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO

-- 1. user timezone (column already existed as nvarchar(50) default 'UTC') ---------
IF COL_LENGTH('urm.user_profiles', 'timezone') IS NULL
    ALTER TABLE urm.user_profiles ADD timezone NVARCHAR(64) NULL;
GO
DECLARE @dc SYSNAME = (SELECT dc.name FROM sys.default_constraints dc JOIN sys.columns c
    ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('urm.user_profiles') AND c.name = 'timezone');
IF @dc IS NOT NULL EXEC('ALTER TABLE urm.user_profiles DROP CONSTRAINT ' + @dc);
UPDATE urm.user_profiles SET timezone = 'Asia/Ho_Chi_Minh' WHERE timezone IS NULL OR timezone IN ('', 'UTC');
ALTER TABLE urm.user_profiles ALTER COLUMN timezone NVARCHAR(64) NOT NULL;
ALTER TABLE urm.user_profiles ADD CONSTRAINT df_user_profiles_timezone DEFAULT 'Asia/Ho_Chi_Minh' FOR timezone;
PRINT 'user_profiles.timezone -> nvarchar(64), default Asia/Ho_Chi_Minh';
GO

-- 2. defaults -> SYSUTCDATETIME() ------------------------------------------------
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N'; '
     + N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' ADD CONSTRAINT ' + QUOTENAME(dc.name)
     + N' DEFAULT SYSUTCDATETIME() FOR ' + QUOTENAME(c.name) + N'; '
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
JOIN sys.tables t ON t.object_id = dc.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE (dc.definition LIKE '%sysdatetime()%' OR dc.definition LIKE '%getdate()%')
  AND dc.definition NOT LIKE '%sysutcdatetime%' AND dc.definition NOT LIKE '%getutcdate%';
IF @sql <> N'' BEGIN EXEC sp_executesql @sql; PRINT 'defaults -> SYSUTCDATETIME()'; END
GO

-- 3+4. one-shot: calendar DATE + VN -> UTC shift ----------------------------------
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE name = '20261004_time_utc_dateonly')
BEGIN
    BEGIN TRAN;

    -- versioning off so the fixes don't create history rows and history columns can change
    ALTER TABLE pro.project      SET (SYSTEM_VERSIONING = OFF);
    ALTER TABLE pro.task         SET (SYSTEM_VERSIONING = OFF);
    ALTER TABLE pro.task_comment SET (SYSTEM_VERSIONING = OFF);

    DECLARE @t NVARCHAR(200), @cols NVARCHAR(400), @c NVARCHAR(100), @q NVARCHAR(MAX);

    -- filtered index on pro.task(start_date, end_date) exists on prod (not dev) and blocks ALTER COLUMN
    DECLARE @had_ix BIT = CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_task_start_end_date' AND object_id = OBJECT_ID('pro.task')) THEN 1 ELSE 0 END;
    IF @had_ix = 1 DROP INDEX ix_task_start_end_date ON pro.task;

    -- 3a. calendar: 17:00 -> next day midnight, then datetime2 -> date (main + history)
    DECLARE cal CURSOR LOCAL FAST_FORWARD FOR
        SELECT v FROM (VALUES ('pro.project'), ('pro.project_history'), ('pro.task'), ('pro.task_history')) x(v);
    OPEN cal; FETCH NEXT FROM cal INTO @t;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @q = N'UPDATE ' + @t + N' SET start_date = DATEADD(hour, 7, start_date) WHERE CAST(start_date AS time) = ''17:00'';'
               + N'UPDATE ' + @t + N' SET end_date   = DATEADD(hour, 7, end_date)   WHERE CAST(end_date   AS time) = ''17:00'';'
               + N'ALTER TABLE ' + @t + N' ALTER COLUMN start_date DATE NULL;'
               + N'ALTER TABLE ' + @t + N' ALTER COLUMN end_date DATE NULL;';
        EXEC sp_executesql @q;
        FETCH NEXT FROM cal INTO @t;
    END
    CLOSE cal; DEALLOCATE cal;

    IF @had_ix = 1
        EXEC('CREATE NONCLUSTERED INDEX ix_task_start_end_date ON pro.task (start_date, end_date) WHERE deleted_at IS NULL');

    -- 4. instants written in Vietnam time -> UTC (-7h)
    DECLARE @shift TABLE (tbl NVARCHAR(200), col NVARCHAR(100));
    INSERT @shift VALUES
        ('pro.project','created_at'),('pro.project','updated_at'),('pro.project','deleted_at'),
        ('pro.project_history','created_at'),('pro.project_history','updated_at'),('pro.project_history','deleted_at'),
        ('pro.task','created_at'),('pro.task','updated_at'),('pro.task','deleted_at'),
        ('pro.task_history','created_at'),('pro.task_history','updated_at'),('pro.task_history','deleted_at'),
        ('pro.task_comment','created_at'),('pro.task_comment','updated_at'),('pro.task_comment','deleted_at'),('pro.task_comment','occurred_at'),
        ('pro.task_comment_history','created_at'),('pro.task_comment_history','updated_at'),('pro.task_comment_history','deleted_at'),('pro.task_comment_history','occurred_at'),
        ('log.log','occur_at'),('log.log','created_at'),('log.log','updated_at'),('log.log','deleted_at'),
        ('log.track','created_at'),('log.track','updated_at'),('log.track','deleted_at'),
        ('pro.daily_log','created_at'),('pro.daily_log','updated_at'),('pro.daily_log','deleted_at'),
        ('pro.daily_log_field_template','created_at'),('pro.daily_log_field_template','updated_at'),('pro.daily_log_field_template','deleted_at'),
        ('k.question','srs_next_review_at');
    DECLARE sh CURSOR LOCAL FAST_FORWARD FOR SELECT tbl, col FROM @shift;
    OPEN sh; FETCH NEXT FROM sh INTO @t, @c;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF COL_LENGTH(@t, @c) IS NOT NULL
        BEGIN
            SET @q = N'UPDATE ' + @t + N' SET ' + QUOTENAME(@c) + N' = DATEADD(hour, -7, ' + QUOTENAME(@c) + N') WHERE ' + QUOTENAME(@c) + N' IS NOT NULL;';
            EXEC sp_executesql @q;
        END
        FETCH NEXT FROM sh INTO @t, @c;
    END
    CLOSE sh; DEALLOCATE sh;

    ALTER TABLE pro.project      SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.project_history));
    ALTER TABLE pro.task         SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.task_history));
    ALTER TABLE pro.task_comment SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.task_comment_history));

    INSERT dbo.schema_migrations (name) VALUES ('20261004_time_utc_dateonly');
    COMMIT;
    PRINT 'calendar -> DATE, VN -> UTC shift applied';
END
ELSE
    PRINT 'skip: 20261004_time_utc_dateonly already applied';
GO
