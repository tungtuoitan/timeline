-- =============================================
-- System-versioned (temporal) tables for pro.project, pro.task, pro.task_comment
-- (TungRoot issue 0095, step 5). Every UPDATE/DELETE keeps the old row in
-- pro.<table>_history automatically.
--
-- Period columns are HIDDEN so SELECT * / INSERT without column list and EF
-- (explicit column lists) keep working unchanged. Period times are UTC
-- (SYSUTCDATETIME), unlike app timestamps (Vietnam time).
--
-- Query history:
--   SELECT * , valid_from, valid_to FROM pro.task FOR SYSTEM_TIME ALL WHERE id = 123 ORDER BY valid_from;
--   SELECT * FROM pro.task FOR SYSTEM_TIME AS OF '2026-10-04T07:00:00' WHERE id = 123;  -- UTC
--
-- Later schema changes: ADD COLUMN works with versioning on (propagates to
-- history). sp_rename / DROP COLUMN need SYSTEM_VERSIONING = OFF first, then
-- apply the same change to the history table, then ON again.
--
-- Rollback (per table, e.g. pro.task):
--   ALTER TABLE pro.task SET (SYSTEM_VERSIONING = OFF);
--   ALTER TABLE pro.task DROP PERIOD FOR SYSTEM_TIME;
--   ALTER TABLE pro.task DROP CONSTRAINT df_task_valid_from, df_task_valid_to;
--   ALTER TABLE pro.task DROP COLUMN valid_from, valid_to;
--   DROP TABLE pro.task_history;
--
-- Idempotent: safe to re-run.
-- =============================================

-- pro.project
IF OBJECTPROPERTY(OBJECT_ID('pro.project'), 'TableTemporalType') = 0
BEGIN
    IF COL_LENGTH('pro.project', 'valid_from') IS NULL
        ALTER TABLE pro.project ADD
            valid_from DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
                CONSTRAINT df_project_valid_from DEFAULT SYSUTCDATETIME(),
            valid_to DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
                CONSTRAINT df_project_valid_to DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
            PERIOD FOR SYSTEM_TIME (valid_from, valid_to);
    ALTER TABLE pro.project SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.project_history));
    PRINT 'pro.project: system versioning ON';
END
GO

-- pro.task
IF OBJECTPROPERTY(OBJECT_ID('pro.task'), 'TableTemporalType') = 0
BEGIN
    IF COL_LENGTH('pro.task', 'valid_from') IS NULL
        ALTER TABLE pro.task ADD
            valid_from DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
                CONSTRAINT df_task_valid_from DEFAULT SYSUTCDATETIME(),
            valid_to DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
                CONSTRAINT df_task_valid_to DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
            PERIOD FOR SYSTEM_TIME (valid_from, valid_to);
    ALTER TABLE pro.task SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.task_history));
    PRINT 'pro.task: system versioning ON';
END
GO

-- pro.task_comment
IF OBJECTPROPERTY(OBJECT_ID('pro.task_comment'), 'TableTemporalType') = 0
BEGIN
    IF COL_LENGTH('pro.task_comment', 'valid_from') IS NULL
        ALTER TABLE pro.task_comment ADD
            valid_from DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
                CONSTRAINT df_task_comment_valid_from DEFAULT SYSUTCDATETIME(),
            valid_to DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
                CONSTRAINT df_task_comment_valid_to DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
            PERIOD FOR SYSTEM_TIME (valid_from, valid_to);
    ALTER TABLE pro.task_comment SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.task_comment_history));
    PRINT 'pro.task_comment: system versioning ON';
END
GO
