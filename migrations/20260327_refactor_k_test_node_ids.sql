-- ============================================================
-- Migration: Refactor k.test — drop node_ids, create k.test_node
-- Date: 2026-03-27
-- ============================================================

-- ── 1. Create k.test_node ────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'k' AND t.name = 'test_node')
BEGIN
    CREATE TABLE [k].[test_node] (
        [id]      INT IDENTITY(1,1) NOT NULL,
        [test_id] INT NOT NULL,
        [node_id] INT NOT NULL,

        CONSTRAINT [PK_k_test_node] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_k_test_node_test_node] UNIQUE ([test_id], [node_id]),
        CONSTRAINT [FK_k_test_node_test] FOREIGN KEY ([test_id])
            REFERENCES [k].[test]([id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_k_test_node_node] ON [k].[test_node] ([node_id]);

    PRINT 'Created table k.test_node';
END
ELSE
BEGIN
    PRINT 'Table k.test_node already exists — skipping';
END

-- ── 2. Migrate existing data: node_ids JSON → k.test_node rows ───────────────
-- Only runs if node_ids column still exists and test_node is empty

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'node_ids')
   AND NOT EXISTS (SELECT 1 FROM [k].[test_node])
BEGIN
    -- Parse JSON arrays from node_ids and insert into test_node
    INSERT INTO [k].[test_node] ([test_id], [node_id])
    SELECT t.[id], n.[value]
    FROM [k].[test] t
    CROSS APPLY OPENJSON(t.[node_ids]) WITH ([value] INT '$') AS n
    WHERE t.[node_ids] IS NOT NULL
      AND t.[node_ids] != '[]';

    PRINT CONCAT('Migrated ', @@ROWCOUNT, ' rows into k.test_node from existing node_ids data');
END

-- ── 3. Drop node_ids column from k.test ──────────────────────────────────────

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'k.test') AND name = 'node_ids')
BEGIN
    ALTER TABLE [k].[test] DROP COLUMN [node_ids];
    PRINT 'Dropped column k.test.node_ids';
END
ELSE
BEGIN
    PRINT 'Column k.test.node_ids does not exist — skipping drop';
END
