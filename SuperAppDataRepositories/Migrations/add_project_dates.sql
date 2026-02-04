-- Add start_date and end_date columns to pro.project table
-- Run this migration on your database

ALTER TABLE pro.project ADD start_date DATETIME2 NULL;
ALTER TABLE pro.project ADD end_date DATETIME2 NULL;

-- Add index for date range queries (optional, for better performance)
-- CREATE INDEX IX_project_dates ON pro.project (start_date, end_date) WHERE deleted_at IS NULL;
