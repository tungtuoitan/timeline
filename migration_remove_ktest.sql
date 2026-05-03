-- ============================================================
-- Migration: Remove k.test layer, link questions to knowledge
-- Run ONCE on the database.
-- ============================================================

BEGIN TRANSACTION;

-- ── 1. Drop old FKs / indexes on k.question ────────────────────────────────

-- Drop the FK from k.question.test_id → k.test.id
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name LIKE 'FK%' AND parent_object_id = OBJECT_ID('k.question')
      AND object_id IN (
          SELECT constraint_object_id FROM sys.foreign_key_columns
          WHERE parent_column_id = COLUMNPROPERTY(OBJECT_ID('k.question'), 'test_id', 'ColumnId')
      )
)
BEGIN
    DECLARE @fk_question NVARCHAR(256);
    SELECT @fk_question = name FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('k.question')
      AND object_id IN (
          SELECT constraint_object_id FROM sys.foreign_key_columns
          WHERE parent_column_id = COLUMNPROPERTY(OBJECT_ID('k.question'), 'test_id', 'ColumnId')
      );
    EXEC('ALTER TABLE k.question DROP CONSTRAINT ' + @fk_question);
END;

-- Drop index on test_id
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_k_question_test' AND object_id = OBJECT_ID('k.question'))
    DROP INDEX IX_k_question_test ON k.question;

-- ── 2. Add knowledge_id to k.question ──────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.question') AND name = 'knowledge_id')
    ALTER TABLE k.question ADD knowledge_id INT NULL;

-- Add FK to k.knowledge
ALTER TABLE k.question
    ADD CONSTRAINT FK_k_question_knowledge
    FOREIGN KEY (knowledge_id) REFERENCES k.knowledge(id) ON DELETE SET NULL;

-- Add index
CREATE INDEX IX_k_question_knowledge ON k.question (knowledge_id);

-- Drop old test_id column
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.question') AND name = 'test_id')
    ALTER TABLE k.question DROP COLUMN test_id;

-- ── 3. Drop old FKs / indexes on k.point_history ───────────────────────────

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('k.point_history')
      AND object_id IN (
          SELECT constraint_object_id FROM sys.foreign_key_columns
          WHERE parent_column_id = COLUMNPROPERTY(OBJECT_ID('k.point_history'), 'test_id', 'ColumnId')
      )
)
BEGIN
    DECLARE @fk_history NVARCHAR(256);
    SELECT @fk_history = name FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('k.point_history')
      AND object_id IN (
          SELECT constraint_object_id FROM sys.foreign_key_columns
          WHERE parent_column_id = COLUMNPROPERTY(OBJECT_ID('k.point_history'), 'test_id', 'ColumnId')
      );
    EXEC('ALTER TABLE k.point_history DROP CONSTRAINT ' + @fk_history);
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_k_point_history_test_user' AND object_id = OBJECT_ID('k.point_history'))
    DROP INDEX IX_k_point_history_test_user ON k.point_history;

-- ── 4. Add knowledge_id to k.point_history ─────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.point_history') AND name = 'knowledge_id')
    ALTER TABLE k.point_history ADD knowledge_id INT NULL;

CREATE INDEX IX_k_point_history_knowledge_user ON k.point_history (knowledge_id, user_id);

-- Drop old test_id column
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.point_history') AND name = 'test_id')
    ALTER TABLE k.point_history DROP COLUMN test_id;

-- ── 5. Drop k.test table (cascade via existing FKs already removed above) ──

IF OBJECT_ID('k.test', 'U') IS NOT NULL
    DROP TABLE k.test;

COMMIT TRANSACTION;
