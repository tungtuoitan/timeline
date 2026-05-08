-- ============================================================
-- Migration: Create k.question_status_history and k.node_status_history
-- Date: 2026-05-08
-- ============================================================

-- ── k.question_status_history ────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'k' AND t.name = 'question_status_history')
BEGIN
    CREATE TABLE [k].[question_status_history] (
        [id]          INT          IDENTITY(1,1) NOT NULL,
        [question_id] INT          NOT NULL,
        [status_code] NVARCHAR(32) NOT NULL,
        [changed_at]  DATETIME2    NOT NULL DEFAULT GETUTCDATE(),
        [user_id]     INT          NULL,

        CONSTRAINT [PK_k_question_status_history] PRIMARY KEY ([id]),
        CONSTRAINT [FK_k_question_status_history_question] FOREIGN KEY ([question_id])
            REFERENCES [k].[question]([id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_k_question_status_history_question_changed]
        ON [k].[question_status_history] ([question_id], [changed_at]);

    PRINT 'Created table k.question_status_history';
END
ELSE
BEGIN
    PRINT 'Table k.question_status_history already exists — skipping create';
END

-- ── k.node_status_history ────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'k' AND t.name = 'node_status_history')
BEGIN
    CREATE TABLE [k].[node_status_history] (
        [id]          INT          IDENTITY(1,1) NOT NULL,
        [node_id]     INT          NOT NULL,
        [status_code] NVARCHAR(32) NOT NULL,
        [changed_at]  DATETIME2    NOT NULL DEFAULT GETUTCDATE(),
        [user_id]     INT          NULL,

        CONSTRAINT [PK_k_node_status_history] PRIMARY KEY ([id]),
        CONSTRAINT [FK_k_node_status_history_node] FOREIGN KEY ([node_id])
            REFERENCES [k].[node]([id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_k_node_status_history_node_changed]
        ON [k].[node_status_history] ([node_id], [changed_at]);

    PRINT 'Created table k.node_status_history';
END
ELSE
BEGIN
    PRINT 'Table k.node_status_history already exists — skipping create';
END

-- ── Backfill: 1 row per existing question / node at its created_at ───────────

IF NOT EXISTS (SELECT 1 FROM [k].[question_status_history])
BEGIN
    INSERT INTO [k].[question_status_history] ([question_id], [status_code], [changed_at], [user_id])
    SELECT [id], ISNULL([status_code], 'learning'), ISNULL([created_at], GETUTCDATE()), NULL
    FROM [k].[question];

    PRINT 'Backfilled k.question_status_history';
END
ELSE
BEGIN
    PRINT 'k.question_status_history already has data — skipping backfill';
END

IF NOT EXISTS (SELECT 1 FROM [k].[node_status_history])
BEGIN
    INSERT INTO [k].[node_status_history] ([node_id], [status_code], [changed_at], [user_id])
    SELECT [id], COALESCE([status_code], 'learning'), ISNULL([created_at], GETUTCDATE()), NULL
    FROM [k].[node];

    PRINT 'Backfilled k.node_status_history';
END
ELSE
BEGIN
    PRINT 'k.node_status_history already has data — skipping backfill';
END
