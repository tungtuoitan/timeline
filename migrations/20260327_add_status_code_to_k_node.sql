-- Migration: Add status_code column to k.node table
-- Date: 2026-03-27
-- Used by: markdown import (statusCode = 'draft'), future workflow states

ALTER TABLE [k].[node]
ADD [status_code] NVARCHAR(50) NULL;
GO
