-- Migration: Reflect facts become free-form (pro schema)
-- Date: 20261114b
-- Description: what is read out of a Reflect row is no longer a fixed kind of number.
--   pro.reflect_observation: value may be empty, plus a text and an item (e.g. which suggestion sentence).
--   pro.reflect_metric: a drawing hint (shape) and a sensitive flag (hidden behind privacy mode).
-- Claude writes these rows while the user talks (scripts/superapp/reflect.ps1 facts); the server only reads them.

IF COL_LENGTH('pro.reflect_metric', 'shape') IS NULL
    ALTER TABLE [pro].[reflect_metric] ADD shape NVARCHAR(16) NOT NULL CONSTRAINT DF_pro_reflect_metric_shape DEFAULT N'auto';  -- auto | flag | count | level | items | range | list
GO
IF COL_LENGTH('pro.reflect_metric', 'sensitive') IS NULL
    ALTER TABLE [pro].[reflect_metric] ADD sensitive BIT NOT NULL CONSTRAINT DF_pro_reflect_metric_sensitive DEFAULT 0;
GO

ALTER TABLE [pro].[reflect_observation] ALTER COLUMN [value] DECIMAL(18,4) NULL;
GO
IF COL_LENGTH('pro.reflect_observation', 'value_text') IS NULL
    ALTER TABLE [pro].[reflect_observation] ADD value_text NVARCHAR(400) NULL;
GO
IF COL_LENGTH('pro.reflect_observation', 'item') IS NULL
    ALTER TABLE [pro].[reflect_observation] ADD item NVARCHAR(200) NOT NULL CONSTRAINT DF_pro_reflect_observation_item DEFAULT N'';
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_pro_reflect_observation_entry_metric_day' AND object_id = OBJECT_ID('pro.reflect_observation'))
    DROP INDEX UX_pro_reflect_observation_entry_metric_day ON [pro].[reflect_observation];
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_pro_reflect_observation_entry_metric_day_item' AND object_id = OBJECT_ID('pro.reflect_observation'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_pro_reflect_observation_entry_metric_day_item
        ON [pro].[reflect_observation] (entry_id, metric_id, occurred_on, item);
GO

PRINT 'Migration 20261114b_reflect_facts completed successfully.';
GO
