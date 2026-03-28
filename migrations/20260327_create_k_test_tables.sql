-- ============================================================
-- Migration: Create k.test and k.point_history tables
-- Date: 2026-03-27
-- ============================================================

-- ── k.test ───────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'k' AND t.name = 'test')
BEGIN
    CREATE TABLE [k].[test] (
        [id]           INT            IDENTITY(1,1) NOT NULL,
        [knowledge_id] INT            NOT NULL,
        [user_id]      INT            NOT NULL,
        [title]        NVARCHAR(500)  NOT NULL,
        [level]        INT            NOT NULL DEFAULT 1,
        [mode]         NVARCHAR(50)   NULL     DEFAULT 'standard',
        [status]       NVARCHAR(50)   NULL     DEFAULT 'active',
        [node_ids]     NVARCHAR(MAX)  NULL,
        [created_at]   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [updated_at]   DATETIME2      NULL,
        [deleted_at]   DATETIME2      NULL,

        CONSTRAINT [PK_k_test] PRIMARY KEY ([id]),
        CONSTRAINT [FK_k_test_knowledge] FOREIGN KEY ([knowledge_id])
            REFERENCES [k].[knowledge]([id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_k_test_knowledge_user]
        ON [k].[test] ([knowledge_id], [user_id], [deleted_at])
        WHERE [deleted_at] IS NULL;

    PRINT 'Created table k.test';
END
ELSE
BEGIN
    PRINT 'Table k.test already exists — adding missing columns if needed';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'user_id')
    BEGIN
        ALTER TABLE [k].[test] ADD [user_id] INT NOT NULL DEFAULT 0;
        PRINT '  + Added column: user_id';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'mode')
    BEGIN
        ALTER TABLE [k].[test] ADD [mode] NVARCHAR(50) NULL DEFAULT 'standard';
        PRINT '  + Added column: mode';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'node_ids')
    BEGIN
        ALTER TABLE [k].[test] ADD [node_ids] NVARCHAR(MAX) NULL;
        PRINT '  + Added column: node_ids';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'status')
    BEGIN
        ALTER TABLE [k].[test] ADD [status] NVARCHAR(50) NULL DEFAULT 'active';
        PRINT '  + Added column: status';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'level')
    BEGIN
        ALTER TABLE [k].[test] ADD [level] INT NOT NULL DEFAULT 1;
        PRINT '  + Added column: level';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'title')
    BEGIN
        ALTER TABLE [k].[test] ADD [title] NVARCHAR(500) NOT NULL DEFAULT '';
        PRINT '  + Added column: title';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'updated_at')
    BEGIN
        ALTER TABLE [k].[test] ADD [updated_at] DATETIME2 NULL;
        PRINT '  + Added column: updated_at';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'deleted_at')
    BEGIN
        ALTER TABLE [k].[test] ADD [deleted_at] DATETIME2 NULL;
        PRINT '  + Added column: deleted_at';
    END
END

-- ── k.point_history ──────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'k' AND t.name = 'point_history')
BEGIN
    CREATE TABLE [k].[point_history] (
        [id]          INT            IDENTITY(1,1) NOT NULL,
        [test_id]     INT            NOT NULL,
        [user_id]     INT            NOT NULL,
        [node_id]     INT            NULL,
        [answer_text] NVARCHAR(MAX)  NULL,
        [point]       INT            NOT NULL DEFAULT 0,
        [created_at]  DATETIME2      NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_k_point_history] PRIMARY KEY ([id]),
        CONSTRAINT [FK_k_point_history_test] FOREIGN KEY ([test_id])
            REFERENCES [k].[test]([id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_k_point_history_test_user]
        ON [k].[point_history] ([test_id], [user_id]);

    CREATE INDEX [IX_k_point_history_user_node]
        ON [k].[point_history] ([user_id], [node_id]);

    PRINT 'Created table k.point_history';
END
ELSE
BEGIN
    PRINT 'Table k.point_history already exists — no changes needed';
END
