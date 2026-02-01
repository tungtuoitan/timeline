-- Migration: Add Google Drive support to users and files tables
-- Date: 2026-02-01
-- Description: Store Google OAuth tokens for Drive access, add google_drive_file_id to files

-- =====================================================
-- 1. Add Google OAuth token columns to urm.users table
-- =====================================================

-- Add google_access_token column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('urm.users') AND name = 'google_access_token')
BEGIN
    ALTER TABLE urm.users ADD google_access_token NVARCHAR(2048) NULL;
    PRINT 'Added column: google_access_token to urm.users';
END
ELSE
BEGIN
    PRINT 'Column google_access_token already exists in urm.users';
END
GO

-- Add google_refresh_token column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('urm.users') AND name = 'google_refresh_token')
BEGIN
    ALTER TABLE urm.users ADD google_refresh_token NVARCHAR(512) NULL;
    PRINT 'Added column: google_refresh_token to urm.users';
END
ELSE
BEGIN
    PRINT 'Column google_refresh_token already exists in urm.users';
END
GO

-- Add google_token_expires_at column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('urm.users') AND name = 'google_token_expires_at')
BEGIN
    ALTER TABLE urm.users ADD google_token_expires_at DATETIME2 NULL;
    PRINT 'Added column: google_token_expires_at to urm.users';
END
ELSE
BEGIN
    PRINT 'Column google_token_expires_at already exists in urm.users';
END
GO

-- =====================================================
-- 2. Add google_drive_file_id column to dbo.files table
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.files') AND name = 'google_drive_file_id')
BEGIN
    ALTER TABLE dbo.files ADD google_drive_file_id NVARCHAR(100) NULL;
    PRINT 'Added column: google_drive_file_id to dbo.files';
END
ELSE
BEGIN
    PRINT 'Column google_drive_file_id already exists in dbo.files';
END
GO

-- =====================================================
-- 3. Add index for google_drive_file_id
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_files_drive_file_id' AND object_id = OBJECT_ID('dbo.files'))
BEGIN
    CREATE INDEX IX_files_drive_file_id ON dbo.files(google_drive_file_id) WHERE google_drive_file_id IS NOT NULL;
    PRINT 'Created index: IX_files_drive_file_id';
END
GO

PRINT 'Migration completed successfully!';
GO
