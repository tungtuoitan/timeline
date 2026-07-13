-- Migration: Create DailyLog tables (pro schema)
-- Date: 20260713
-- Description: pro.daily_log (1 row/user/day with JSON blob) + pro.daily_log_field_template (per-user global form definition)

-- =====================================================
-- Step 1: pro.daily_log_field_template
-- =====================================================
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'pro' AND TABLE_NAME = 'daily_log_field_template'
)
BEGIN
    CREATE TABLE [pro].[daily_log_field_template] (
        id           INT IDENTITY(1,1) NOT NULL,
        user_id      INT NOT NULL,
        section      NVARCHAR(16) NOT NULL,       -- 'input' | 'output'
        field_key    NVARCHAR(64) NOT NULL,       -- stable JSON key: 'general', 'note', 'emotion_note', ...
        label        NVARCHAR(128) NOT NULL,
        field_type   NVARCHAR(16) NOT NULL,       -- 'text' | 'longText' | 'checkbox' | 'number'
        sort_order   INT NOT NULL CONSTRAINT DF_pro_daily_log_field_template_sort_order DEFAULT 0,
        created_at   DATETIME2 NOT NULL CONSTRAINT DF_pro_daily_log_field_template_created_at DEFAULT SYSDATETIME(),
        updated_at   DATETIME2 NOT NULL CONSTRAINT DF_pro_daily_log_field_template_updated_at DEFAULT SYSDATETIME(),
        deleted_at   DATETIME2 NULL,
        CONSTRAINT PK_pro_daily_log_field_template PRIMARY KEY (id)
    );
    PRINT 'Created table: pro.daily_log_field_template';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_pro_daily_log_field_template_user_section' AND object_id = OBJECT_ID('pro.daily_log_field_template'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_pro_daily_log_field_template_user_section
        ON [pro].[daily_log_field_template] (user_id, section);
    PRINT 'Created index: IX_pro_daily_log_field_template_user_section';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_pro_daily_log_field_template_user_section_key' AND object_id = OBJECT_ID('pro.daily_log_field_template'))
BEGIN
    CREATE UNIQUE INDEX UX_pro_daily_log_field_template_user_section_key
        ON [pro].[daily_log_field_template] (user_id, section, field_key)
        WHERE deleted_at IS NULL;
    PRINT 'Created unique index: UX_pro_daily_log_field_template_user_section_key';
END
GO

-- =====================================================
-- Step 2: pro.daily_log
-- =====================================================
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'pro' AND TABLE_NAME = 'daily_log'
)
BEGIN
    CREATE TABLE [pro].[daily_log] (
        id           INT IDENTITY(1,1) NOT NULL,
        user_id      INT NOT NULL,
        log_date     DATE NOT NULL,
        values_json  NVARCHAR(MAX) NOT NULL CONSTRAINT DF_pro_daily_log_values_json DEFAULT N'{}',
        created_at   DATETIME2 NOT NULL CONSTRAINT DF_pro_daily_log_created_at DEFAULT SYSDATETIME(),
        updated_at   DATETIME2 NOT NULL CONSTRAINT DF_pro_daily_log_updated_at DEFAULT SYSDATETIME(),
        deleted_at   DATETIME2 NULL,
        CONSTRAINT PK_pro_daily_log PRIMARY KEY (id),
        CONSTRAINT CK_pro_daily_log_values_json_is_json CHECK (ISJSON(values_json) = 1)
    );
    PRINT 'Created table: pro.daily_log';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_pro_daily_log_user_date' AND object_id = OBJECT_ID('pro.daily_log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_pro_daily_log_user_date
        ON [pro].[daily_log] (user_id, log_date DESC);
    PRINT 'Created index: IX_pro_daily_log_user_date';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_pro_daily_log_user_date' AND object_id = OBJECT_ID('pro.daily_log'))
BEGIN
    CREATE UNIQUE INDEX UX_pro_daily_log_user_date
        ON [pro].[daily_log] (user_id, log_date)
        WHERE deleted_at IS NULL;
    PRINT 'Created unique index: UX_pro_daily_log_user_date';
END
GO

PRINT 'Migration 20260713_create_daily_log_tables completed successfully.';
GO
