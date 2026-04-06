-- Add soft-delete support to k.question
ALTER TABLE [k].[question]
    ADD [deleted_at] DATETIME2 NULL;
GO
