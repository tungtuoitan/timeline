-- Migration: Add template_json column to pro.daily_log
-- Date: 20260713
-- Description: Snapshot the active field template into each log so past logs keep their own structure
--              even when the user later edits the default template.

IF COL_LENGTH('pro.daily_log', 'template_json') IS NULL
BEGIN
    ALTER TABLE [pro].[daily_log] ADD template_json NVARCHAR(MAX) NULL;
    PRINT 'Added column: pro.daily_log.template_json';
END
GO

-- Optional: enforce JSON when non-null (skipped for simplicity — existing NULL rows would fail the check
-- on CREATE, and app already validates before insert).
PRINT 'Migration 20260713_add_template_json_to_daily_log completed successfully.';
GO
