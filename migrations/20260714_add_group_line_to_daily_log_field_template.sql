-- Migration: Add group_order, group_label, line_order columns to pro.daily_log_field_template
-- Date: 20260714
-- Description: Replace width_mode with group/line layout fields for form template builder (Section > Group > Line > Field).

IF COL_LENGTH('pro.daily_log_field_template', 'group_order') IS NULL
BEGIN
    ALTER TABLE [pro].[daily_log_field_template] ADD group_order INT NULL;
    PRINT 'Added column: pro.daily_log_field_template.group_order';
END
GO

IF COL_LENGTH('pro.daily_log_field_template', 'group_label') IS NULL
BEGIN
    ALTER TABLE [pro].[daily_log_field_template] ADD group_label NVARCHAR(100) NULL;
    PRINT 'Added column: pro.daily_log_field_template.group_label';
END
GO

IF COL_LENGTH('pro.daily_log_field_template', 'line_order') IS NULL
BEGIN
    ALTER TABLE [pro].[daily_log_field_template] ADD line_order INT NULL;
    PRINT 'Added column: pro.daily_log_field_template.line_order';
END
GO

PRINT 'Migration 20260714_add_group_line_to_daily_log_field_template completed successfully.';
GO
