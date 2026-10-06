-- Migration: TOTP second factor + sensitive tasks
-- Date: 20261006
-- Issue: TungRoot #1489 (mở full mode homepage cần mã TOTP; API riêng tư chỉ trả khi có vé mở khoá)
-- Description:
--   1. auth.user_totp — 1 row per user: active/pending secret (base32), last used time step (replay
--      guard), wrong-code counter + lock (3 wrong → 10 min, 5 wrong → 1 day).
--   2. pro.task.is_sensitive — private trackers; homepage APIs hide them without an unlock token.
--      pro.task is system-versioned: ADD with a default also adds the column to pro.task_history.
--   Timestamps are UTC (#1450) → SYSUTCDATETIME().

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'auth' AND TABLE_NAME = 'user_totp')
BEGIN
    CREATE TABLE [auth].[user_totp] (
        user_id         INT NOT NULL,
        secret          NVARCHAR(64) NULL,
        pending_secret  NVARCHAR(64) NULL,
        enabled_at      DATETIME2 NULL,
        last_used_step  BIGINT NULL,
        failed_count    INT NOT NULL CONSTRAINT DF_auth_user_totp_failed_count DEFAULT 0,
        locked_until    DATETIME2 NULL,
        created_at      DATETIME2 NOT NULL CONSTRAINT DF_auth_user_totp_created_at DEFAULT SYSUTCDATETIME(),
        updated_at      DATETIME2 NOT NULL CONSTRAINT DF_auth_user_totp_updated_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_auth_user_totp PRIMARY KEY (user_id),
        CONSTRAINT FK_auth_user_totp_user FOREIGN KEY (user_id) REFERENCES [urm].[users] (id) ON DELETE CASCADE
    );
    PRINT 'Created table: auth.user_totp';
END
GO

IF COL_LENGTH('pro.task', 'is_sensitive') IS NULL
BEGIN
    ALTER TABLE [pro].[task] ADD is_sensitive BIT NOT NULL CONSTRAINT DF_pro_task_is_sensitive DEFAULT 0;
    PRINT 'Added column: pro.task.is_sensitive';
END
GO

-- Rollback:
-- DROP TABLE IF EXISTS [auth].[user_totp];
-- ALTER TABLE [pro].[task] SET (SYSTEM_VERSIONING = OFF);
-- ALTER TABLE [pro].[task] DROP CONSTRAINT DF_pro_task_is_sensitive;
-- ALTER TABLE [pro].[task] DROP COLUMN is_sensitive;
-- ALTER TABLE [pro].[task_history] DROP COLUMN is_sensitive;
-- ALTER TABLE [pro].[task] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [pro].[task_history]));
