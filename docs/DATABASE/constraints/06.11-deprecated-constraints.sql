-- ============================================
-- FILE: constraints/06.11-deprecated-constraints.sql
-- PURPOSE: ⚠️ DEPRECATED - Constraints for deprecated workspace_tag_relationships
-- DEPENDENCIES: relationships/04.01-table.sql (do not use)
-- STATUS: Kept for reference only, replaced by workspace_items in 07-tables-entities.sql
-- ============================================

-- ⚠️⚠️⚠️ WARNING ⚠️⚠️⚠️
-- This file is DEPRECATED and should NOT be executed.
-- Replaced by: workspace_items (07-tables-entities.sql)
-- Migration: See 12-migration-guide.md

-- No additional constraints defined for deprecated table
-- Existing constraints are in relationships/04.01-table.sql
PRINT '⚠️ DEPRECATED: No additional constraints for workspace_tag_relationships';
PRINT '   Replaced by: workspace_items in 07-tables-entities.sql';
PRINT '   Migration: See 12-migration-guide.md';
PRINT '📊 Next step: Run constraints/06.12-business-logic-triggers.sql';
GO