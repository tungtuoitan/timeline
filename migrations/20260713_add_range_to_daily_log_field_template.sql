-- Migration: Add range_min / range_max columns to pro.daily_log_field_template
-- Date: 20260713
-- Description: Support fieldType = "range" (slider) with configurable inclusive bounds.

IF COL_LENGTH('pro.daily_log_field_template', 'range_min') IS NULL
BEGIN
    ALTER TABLE [pro].[daily_log_field_template] ADD range_min FLOAT NULL;
    PRINT 'Added column: pro.daily_log_field_template.range_min';
END
GO

IF COL_LENGTH('pro.daily_log_field_template', 'range_max') IS NULL
BEGIN
    ALTER TABLE [pro].[daily_log_field_template] ADD range_max FLOAT NULL;
    PRINT 'Added column: pro.daily_log_field_template.range_max';
END
GO

PRINT 'Migration 20260713_add_range_to_daily_log_field_template completed successfully.';
GO
