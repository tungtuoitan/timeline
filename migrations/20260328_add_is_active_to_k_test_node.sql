-- ============================================================
-- Migration: Add is_active to k.test_node
-- Date: 2026-03-28
-- Purpose: Allow individual nodes in a test to be toggled on/off
--          Default = 1 (active) — all existing rows are active
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'k.test_node')
      AND name = 'is_active'
)
BEGIN
    ALTER TABLE [k].[test_node]
    ADD [is_active] BIT NOT NULL DEFAULT 1;

    PRINT 'Added column k.test_node.is_active (default 1)';
END
ELSE
BEGIN
    PRINT 'Column k.test_node.is_active already exists — skipping';
END
GO
