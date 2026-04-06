-- Add SRS (Spaced Repetition) columns to k.question
ALTER TABLE [k].[question]
    ADD [srs_interval]        INT       NOT NULL DEFAULT 0,
        [srs_ease_factor]     FLOAT     NOT NULL DEFAULT 2.5,
        [srs_repetitions]     INT       NOT NULL DEFAULT 0,
        [srs_next_review_at]  DATETIME2 NULL;
GO

-- Add response_time_ms to k.point_history
ALTER TABLE [k].[point_history]
    ADD [response_time_ms] INT NULL;
GO

-- Update existing test statuses: 'active' → 'inactive'
UPDATE [k].[test] SET [status] = 'inactive' WHERE [status] = 'active';
GO

-- Index for daily queue lookups
CREATE NONCLUSTERED INDEX [IX_k_question_srs_next_review]
    ON [k].[question] ([srs_next_review_at])
    INCLUDE ([test_id], [is_active])
    WHERE [deleted_at] IS NULL;
GO
