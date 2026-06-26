-- Add context columns to k.question for code snippet shown during daily review
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.question') AND name = 'context')
    ALTER TABLE k.question ADD context NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('k.question') AND name = 'context_question_id')
    ALTER TABLE k.question ADD context_question_id INT NULL;
