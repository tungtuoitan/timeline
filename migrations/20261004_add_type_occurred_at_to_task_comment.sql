-- =============================================
-- pro.task_comment: add type + occurred_at (TungRoot issue 0095, step 4)
--   type        : comment | decision | devlog | track  (validated in TaskCommentService)
--                 existing rows become 'comment' via the default
--   occurred_at : when the thing actually happened (null = use created_at),
--                 lets imports keep old dates and lets a habit be logged late
-- Idempotent: safe to re-run.
--
-- Rollback:
--   DROP INDEX IF EXISTS ix_task_comment_task_type ON pro.task_comment;
--   ALTER TABLE pro.task_comment DROP CONSTRAINT df_task_comment_type;
--   ALTER TABLE pro.task_comment DROP COLUMN type, occurred_at;
-- =============================================

IF COL_LENGTH('pro.task_comment', 'type') IS NULL
BEGIN
    ALTER TABLE pro.task_comment
        ADD type NVARCHAR(20) NOT NULL CONSTRAINT df_task_comment_type DEFAULT 'comment';
    PRINT 'Added pro.task_comment.type';
END
GO

IF COL_LENGTH('pro.task_comment', 'occurred_at') IS NULL
BEGIN
    ALTER TABLE pro.task_comment ADD occurred_at DATETIME2 NULL;
    PRINT 'Added pro.task_comment.occurred_at';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_task_comment_task_type' AND object_id = OBJECT_ID('pro.task_comment'))
BEGIN
    CREATE INDEX ix_task_comment_task_type ON pro.task_comment (task_id, type);
    PRINT 'Created ix_task_comment_task_type';
END
GO
