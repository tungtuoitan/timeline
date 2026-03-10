-- ============================================================
-- KnowledgeTree tables
-- Schema: kt.*
-- Created: 2026-03-09
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'kt')
BEGIN
    EXEC('CREATE SCHEMA kt');
END
GO

-- ── kt.knowledge ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'kt' AND t.name = 'knowledge')
BEGIN
    CREATE TABLE [kt].[knowledge] (
        [id]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_id]     INT NOT NULL,
        [parent_id]   INT NULL REFERENCES [kt].[knowledge]([id]),
        [title]       NVARCHAR(255) NOT NULL,
        [description] NVARCHAR(MAX) NULL,
        [created_at]  DATETIME2 NULL DEFAULT GETUTCDATE(),
        [updated_at]  DATETIME2 NULL DEFAULT GETUTCDATE(),
        [deleted_at]  DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX [IX_kt_knowledge_user_id]   ON [kt].[knowledge] ([user_id]);
    CREATE NONCLUSTERED INDEX [IX_kt_knowledge_parent_id] ON [kt].[knowledge] ([parent_id]);
    CREATE NONCLUSTERED INDEX [IX_kt_knowledge_deleted_at] ON [kt].[knowledge] ([deleted_at]);
END
GO

-- ── kt.card ───────────────────────────────────────────────────
-- Unified card entity (replaces both node & connection).
-- is_definition = 1 → định nghĩa khái niệm
-- is_definition = 0 → mối liên hệ giữa các card khác
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'kt' AND t.name = 'card')
BEGIN
    CREATE TABLE [kt].[card] (
        [id]            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [knowledge_id]  INT NOT NULL REFERENCES [kt].[knowledge]([id]),
        [parent_card_id] INT NULL REFERENCES [kt].[card]([id]),
        [user_id]       INT NOT NULL,
        [keyword]       NVARCHAR(255) NOT NULL DEFAULT '',
        [title]         NVARCHAR(255) NOT NULL,
        [description]   NVARCHAR(MAX) NULL,
        [is_definition] BIT NOT NULL DEFAULT 1,
        [created_at]    DATETIME2 NULL DEFAULT GETUTCDATE(),
        [updated_at]    DATETIME2 NULL DEFAULT GETUTCDATE(),
        [deleted_at]    DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX [IX_kt_card_knowledge_id]   ON [kt].[card] ([knowledge_id]);
    CREATE NONCLUSTERED INDEX [IX_kt_card_parent_card_id] ON [kt].[card] ([parent_card_id]);
    CREATE NONCLUSTERED INDEX [IX_kt_card_user_id]        ON [kt].[card] ([user_id]);
    CREATE NONCLUSTERED INDEX [IX_kt_card_deleted_at]     ON [kt].[card] ([deleted_at]);
END
GO

-- ── kt.card_link ──────────────────────────────────────────────
-- Many-to-many: relation card ↔ definition cards it links
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'kt' AND t.name = 'card_link')
BEGIN
    CREATE TABLE [kt].[card_link] (
        [id]              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [source_card_id]  INT NOT NULL REFERENCES [kt].[card]([id]),  -- relation card
        [target_card_id]  INT NOT NULL REFERENCES [kt].[card]([id]),  -- definition card
        CONSTRAINT [UQ_kt_card_link] UNIQUE ([source_card_id], [target_card_id])
    );

    CREATE NONCLUSTERED INDEX [IX_kt_card_link_source] ON [kt].[card_link] ([source_card_id]);
    CREATE NONCLUSTERED INDEX [IX_kt_card_link_target] ON [kt].[card_link] ([target_card_id]);
END
GO
