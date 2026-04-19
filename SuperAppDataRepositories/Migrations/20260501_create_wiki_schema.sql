-- =============================================================================
-- Wiki Schema Migration
-- Created: 2026-05-01
-- Description: Creates wiki schema with keywords, infos, synonyms, and junction table
-- =============================================================================

SET QUOTED_IDENTIFIER ON;
GO

-- ── Schema ────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'wiki')
BEGIN
    EXEC('CREATE SCHEMA [wiki]');
    PRINT 'Created schema [wiki]';
END

-- ── wiki.keyword ──────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'wiki' AND t.name = 'keyword'
)
BEGIN
    CREATE TABLE [wiki].[keyword] (
        [id]             INT           IDENTITY(1,1) NOT NULL,
        [user_id]        INT           NOT NULL,
        [name]           NVARCHAR(255) NOT NULL,
        [description]    NVARCHAR(MAX) NULL,
        [icon_base64]    NVARCHAR(MAX) NULL,   -- base64 webp data URL (48×48)
        [views]          INT           NOT NULL DEFAULT 0,
        [reads]          INT           NOT NULL DEFAULT 0,
        [edits]          INT           NOT NULL DEFAULT 0,
        [pos_x]          FLOAT         NULL,
        [pos_y]          FLOAT         NULL,
        [pinned_position] BIT          NOT NULL DEFAULT 0,
        [created_at]     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        [updated_at]     DATETIME2     NULL,
        [deleted_at]     DATETIME2     NULL,
        CONSTRAINT [PK_wiki_keyword] PRIMARY KEY ([id]),
        CONSTRAINT [FK_wiki_keyword_users] FOREIGN KEY ([user_id])
            REFERENCES [urm].[users] ([id]) ON DELETE RESTRICT
    );

    CREATE INDEX [IX_wiki_keyword_user] ON [wiki].[keyword] ([user_id])
        WHERE [deleted_at] IS NULL;

    PRINT 'Created table [wiki].[keyword]';
END

-- ── wiki.keyword_synonym ──────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'wiki' AND t.name = 'keyword_synonym'
)
BEGIN
    CREATE TABLE [wiki].[keyword_synonym] (
        [id]         INT           IDENTITY(1,1) NOT NULL,
        [keyword_id] INT           NOT NULL,
        [synonym]    NVARCHAR(255) NOT NULL,
        CONSTRAINT [PK_wiki_keyword_synonym] PRIMARY KEY ([id]),
        CONSTRAINT [FK_wiki_keyword_synonym_keyword] FOREIGN KEY ([keyword_id])
            REFERENCES [wiki].[keyword] ([id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_wiki_keyword_synonym] UNIQUE ([keyword_id], [synonym])
    );

    CREATE INDEX [IX_wiki_keyword_synonym_keyword] ON [wiki].[keyword_synonym] ([keyword_id]);

    PRINT 'Created table [wiki].[keyword_synonym]';
END

-- ── wiki.info ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'wiki' AND t.name = 'info'
)
BEGIN
    CREATE TABLE [wiki].[info] (
        [id]         INT           IDENTITY(1,1) NOT NULL,
        [user_id]    INT           NOT NULL,
        [title]      NVARCHAR(500) NOT NULL,
        [content]    NVARCHAR(MAX) NOT NULL,
        [created_at] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        [updated_at] DATETIME2     NULL,
        [deleted_at] DATETIME2     NULL,
        CONSTRAINT [PK_wiki_info] PRIMARY KEY ([id]),
        CONSTRAINT [FK_wiki_info_users] FOREIGN KEY ([user_id])
            REFERENCES [urm].[users] ([id]) ON DELETE RESTRICT
    );

    CREATE INDEX [IX_wiki_info_user] ON [wiki].[info] ([user_id])
        WHERE [deleted_at] IS NULL;

    PRINT 'Created table [wiki].[info]';
END

-- ── wiki.info_keyword (junction) ──────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'wiki' AND t.name = 'info_keyword'
)
BEGIN
    CREATE TABLE [wiki].[info_keyword] (
        [info_id]    INT NOT NULL,
        [keyword_id] INT NOT NULL,
        CONSTRAINT [PK_wiki_info_keyword] PRIMARY KEY ([info_id], [keyword_id]),
        CONSTRAINT [FK_wiki_info_keyword_info] FOREIGN KEY ([info_id])
            REFERENCES [wiki].[info] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_wiki_info_keyword_keyword] FOREIGN KEY ([keyword_id])
            REFERENCES [wiki].[keyword] ([id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_wiki_info_keyword_keyword] ON [wiki].[info_keyword] ([keyword_id]);

    PRINT 'Created table [wiki].[info_keyword]';
END

PRINT 'Wiki schema migration complete.';
