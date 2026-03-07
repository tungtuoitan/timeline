-- Migration: 20260307_alter_lifelog_track_emoji_length.sql
-- Description: Increase emoji column length to support image paths (e.g. /sleep-late.png)

ALTER TABLE [log].[track]
    ALTER COLUMN emoji NVARCHAR(255) NULL;

PRINT 'Altered log.track.emoji to NVARCHAR(255)';
