-- =============================================
-- Migration: Add HardDeletedAt to Keywords table
-- Description: Add soft delete column for heading keywords
-- Author: Claude Code
-- Date: 2026-01-04
-- =============================================

USE [SuperApp-dev];
GO

-- Step 1: Add HardDeletedAt column
ALTER TABLE [dbo].[Keywords]
ADD [HardDeletedAt] DATETIME2 NULL;
GO

PRINT 'Added HardDeletedAt column to dbo.Keywords';
GO

-- Step 2: Create index on HardDeletedAt for performance
CREATE NONCLUSTERED INDEX [IX_Keywords_HardDeletedAt]
ON [dbo].[Keywords] ([HardDeletedAt])
WHERE [HardDeletedAt] IS NOT NULL;
GO

PRINT 'Created filtered index on HardDeletedAt';
GO

-- Step 3: Update GetKeywords queries to exclude hard deleted keywords
-- Note: Application code should add WHERE HardDeletedAt IS NULL in queries

PRINT 'Migration completed successfully!';
PRINT 'IMPORTANT: Update queries to filter out hard deleted keywords:';
PRINT '  WHERE HardDeletedAt IS NULL';
GO
