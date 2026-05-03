-- Add is_draft column to k.question
-- Marks a question as a work-in-progress draft; excluded from SRS review sessions

ALTER TABLE k.question
ADD is_draft BIT NOT NULL DEFAULT 0;
