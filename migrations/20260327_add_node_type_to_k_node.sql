-- Migration: Add node_type column to k.node table
-- Date: 2026-03-27
-- Values: 'entity' | 'question' | NULL (NULL = no type assigned)

ALTER TABLE [k].[node]
ADD [node_type] NVARCHAR(50) NULL;
GO
