-- Migration: Create LifeLog tables
-- Date: 20260306
-- Description: Create log schema with track and log tables for LifeLog feature

-- =====================================================
-- Step 1: Create log schema if not exists
-- =====================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'log')
BEGIN
    EXEC('CREATE SCHEMA [log]');
    PRINT 'Created schema: log';
END
GO

-- =====================================================
-- Step 2: Create log.track table
-- =====================================================
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'log'
    AND TABLE_NAME = 'track'
)
BEGIN
    CREATE TABLE [log].[track] (
        id              INT IDENTITY(1,1) NOT NULL,
        user_id         INT NOT NULL,
        name            NVARCHAR(255) NOT NULL,
        emoji           NVARCHAR(10) NULL,
        description     NVARCHAR(MAX) NULL,
        is_sensitive    BIT NOT NULL CONSTRAINT DF_log_track_is_sensitive DEFAULT 0,
        color           NVARCHAR(50) NULL,
        created_at      DATETIME2 NOT NULL CONSTRAINT DF_log_track_created_at DEFAULT SYSDATETIME(),
        updated_at      DATETIME2 NOT NULL CONSTRAINT DF_log_track_updated_at DEFAULT SYSDATETIME(),
        deleted_at      DATETIME2 NULL,
        CONSTRAINT PK_log_track PRIMARY KEY (id)
    );
    PRINT 'Created table: log.track';
END
GO

-- =====================================================
-- Step 3: Create log.log table
-- =====================================================
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'log'
    AND TABLE_NAME = 'log'
)
BEGIN
    CREATE TABLE [log].[log] (
        id              INT IDENTITY(1,1) NOT NULL,
        user_id         INT NOT NULL,
        type            NVARCHAR(50) NOT NULL,   -- track|event|reflection|lesson|mistake|note|moment|progress
        track_id        INT NULL,                -- FK to log.track (for track-type logs)
        title           NVARCHAR(255) NULL,
        description     NVARCHAR(MAX) NULL,
        is_sensitive    BIT NOT NULL CONSTRAINT DF_log_log_is_sensitive DEFAULT 0,
        location        NVARCHAR(255) NULL,
        created_at      DATETIME2 NOT NULL CONSTRAINT DF_log_log_created_at DEFAULT SYSDATETIME(),
        updated_at      DATETIME2 NOT NULL CONSTRAINT DF_log_log_updated_at DEFAULT SYSDATETIME(),
        deleted_at      DATETIME2 NULL,
        CONSTRAINT PK_log_log PRIMARY KEY (id),
        CONSTRAINT FK_log_log_track FOREIGN KEY (track_id) REFERENCES [log].[track](id)
    );
    PRINT 'Created table: log.log';
END
GO

-- =====================================================
-- Step 4: Create indexes
-- =====================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_log_track_user_id' AND object_id = OBJECT_ID('log.track'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_log_track_user_id ON [log].[track] (user_id);
    PRINT 'Created index: IX_log_track_user_id';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_log_track_deleted_at' AND object_id = OBJECT_ID('log.track'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_log_track_deleted_at ON [log].[track] (deleted_at);
    PRINT 'Created index: IX_log_track_deleted_at';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_log_log_user_id' AND object_id = OBJECT_ID('log.log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_log_log_user_id ON [log].[log] (user_id);
    PRINT 'Created index: IX_log_log_user_id';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_log_log_type' AND object_id = OBJECT_ID('log.log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_log_log_type ON [log].[log] (type);
    PRINT 'Created index: IX_log_log_type';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_log_log_created_at' AND object_id = OBJECT_ID('log.log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_log_log_created_at ON [log].[log] (created_at DESC);
    PRINT 'Created index: IX_log_log_created_at';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_log_log_track_id' AND object_id = OBJECT_ID('log.log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_log_log_track_id ON [log].[log] (track_id);
    PRINT 'Created index: IX_log_log_track_id';
END
GO

PRINT 'Migration 20260306_create_lifelog_tables completed successfully.';
GO
