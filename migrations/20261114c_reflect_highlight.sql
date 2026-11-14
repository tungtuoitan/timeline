-- Migration: Reflect rows worth reading (pro schema)
-- Date: 20261114c
-- Description: pro.reflect_entry.is_highlight - set by Claude when it processes a row (talk-log skill): what the user
--   thought vs what happened, lessons, decisions. The Reflect page shows only these rows unless "Show all" is on.

IF COL_LENGTH('pro.reflect_entry', 'is_highlight') IS NULL
    ALTER TABLE [pro].[reflect_entry] ADD is_highlight BIT NOT NULL CONSTRAINT DF_pro_reflect_entry_is_highlight DEFAULT 0;
GO

PRINT 'Migration 20261114c_reflect_highlight completed successfully.';
GO
