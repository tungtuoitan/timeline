-- Migration: 20260307_add_occur_at_to_lifelog_log.sql
-- Description: Add occur_at column to log.log — user-editable "when it happened"
-- Defaults to created_at value for existing rows

ALTER TABLE [log].[log]
    ADD occur_at DATETIME2 NULL;

-- Backfill: set occur_at = created_at for all existing rows
UPDATE [log].[log]
SET occur_at = created_at
WHERE occur_at IS NULL;

PRINT 'Added occur_at column to log.log and backfilled from created_at';
