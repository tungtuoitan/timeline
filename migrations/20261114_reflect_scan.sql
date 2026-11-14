-- Migration: Reflect nightly scan (pro schema)
-- Date: 20261114
-- Description: auto tag + scan state on pro.reflect_entry, metric registry pro.reflect_metric,
--              numbers read out of the text pro.reflect_observation (one row per entry/metric/day)

IF COL_LENGTH('pro.reflect_entry', 'tag') IS NULL
    ALTER TABLE [pro].[reflect_entry] ADD tag NVARCHAR(32) NULL;
GO
IF COL_LENGTH('pro.reflect_entry', 'scanned_hash') IS NULL
    ALTER TABLE [pro].[reflect_entry] ADD scanned_hash NVARCHAR(64) NULL;
GO
IF COL_LENGTH('pro.reflect_entry', 'scanned_at') IS NULL
    ALTER TABLE [pro].[reflect_entry] ADD scanned_at DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'pro' AND TABLE_NAME = 'reflect_metric')
BEGIN
    CREATE TABLE [pro].[reflect_metric] (
        id          INT IDENTITY(1,1) NOT NULL,
        user_id     INT           NOT NULL,
        code        NVARCHAR(64)  NOT NULL,   -- stable slug, e.g. run_km
        label       NVARCHAR(128) NOT NULL,   -- shown on the chart title
        unit        NVARCHAR(32)  NOT NULL CONSTRAINT DF_pro_reflect_metric_unit DEFAULT N'',
        aggregate   NVARCHAR(8)   NOT NULL CONSTRAINT DF_pro_reflect_metric_aggregate DEFAULT N'sum',  -- sum | avg | max, how several values of one day combine
        created_at  DATETIME2     NOT NULL CONSTRAINT DF_pro_reflect_metric_created_at DEFAULT SYSUTCDATETIME(),
        deleted_at  DATETIME2     NULL,
        CONSTRAINT PK_pro_reflect_metric PRIMARY KEY (id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX UX_pro_reflect_metric_user_code ON [pro].[reflect_metric] (user_id, code);
    PRINT 'Created table: pro.reflect_metric';
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'pro' AND TABLE_NAME = 'reflect_observation')
BEGIN
    CREATE TABLE [pro].[reflect_observation] (
        id          BIGINT IDENTITY(1,1) NOT NULL,
        user_id     INT           NOT NULL,
        entry_id    NVARCHAR(36)  NOT NULL,
        metric_id   INT           NOT NULL,
        occurred_on DATE          NOT NULL,   -- calendar day the number belongs to (user's timezone)
        [value]     DECIMAL(18,4) NOT NULL,
        quote       NVARCHAR(400) NOT NULL CONSTRAINT DF_pro_reflect_observation_quote DEFAULT N'',  -- the words it was read from
        created_at  DATETIME2     NOT NULL CONSTRAINT DF_pro_reflect_observation_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_pro_reflect_observation PRIMARY KEY (id),
        CONSTRAINT FK_pro_reflect_observation_metric FOREIGN KEY (metric_id) REFERENCES [pro].[reflect_metric](id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX UX_pro_reflect_observation_entry_metric_day ON [pro].[reflect_observation] (entry_id, metric_id, occurred_on);
    CREATE NONCLUSTERED INDEX IX_pro_reflect_observation_user_metric_day ON [pro].[reflect_observation] (user_id, metric_id, occurred_on);
    PRINT 'Created table: pro.reflect_observation';
END
GO

PRINT 'Migration 20261114_reflect_scan completed successfully.';
GO
