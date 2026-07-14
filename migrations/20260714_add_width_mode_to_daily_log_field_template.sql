-- Migration: Add width_mode column to pro.daily_log_field_template
-- Date: 20260714
-- Description: Support configurable width mode for daily log field templates.

IF COL_LENGTH('pro.daily_log_field_template', 'width_mode') IS NULL
BEGIN
    ALTER TABLE [pro].[daily_log_field_template] ADD width_mode NVARCHAR(16) NULL;
    PRINT 'Added column: pro.daily_log_field_template.width_mode';
END
GO

PRINT 'Migration 20260714_add_width_mode_to_daily_log_field_template completed successfully.';
GO
