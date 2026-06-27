-- Add context + directives columns to k.question
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.question') AND name = 'context')
    ALTER TABLE k.question ADD context NVARCHAR(MAX) NULL;

-- Remove borrowed-context FK (replaced by scope-based directives)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.question') AND name = 'context_question_id')
    ALTER TABLE k.question DROP COLUMN context_question_id;

-- JSON array of directive strings e.g. '["open-context"]', '["close-context"]'
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.question') AND name = 'directives')
    ALTER TABLE k.question ADD directives NVARCHAR(MAX) NULL;
