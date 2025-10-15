/*
============================================
RUN ALL PROCEDURES - Master Script
============================================
Description: Execute all stored procedures in the correct order
Created: 2024
Author: SuperApp Team
============================================
*/

PRINT '============================================';
PRINT 'Starting Procedure Deployment...';
PRINT '============================================';
PRINT '';

-- ============================================
-- 1. WORKSPACE PROCEDURES (12)
-- ============================================
PRINT '1. Deploying Workspace Procedures...';
:r procedures\workspace\usp_i_workspace.sql
:r procedures\workspace\usp_s_workspace.sql
:r procedures\workspace\usp_s_workspaces.sql
:r procedures\workspace\usp_u_workspace.sql
:r procedures\workspace\usp_d_workspace.sql
:r procedures\workspace\usp_add_workspace_member.sql
:r procedures\workspace\usp_remove_workspace_member.sql
:r procedures\workspace\usp_change_workspace_member_role.sql
:r procedures\workspace\usp_s_workspace_stats.sql
:r procedures\workspace\usp_search_workspaces.sql
:r procedures\workspace\usp_update_workspace_access.sql
:r procedures\workspace\usp_clone_workspace.sql
PRINT '';

-- ============================================
-- 2. ITEMS PROCEDURES (10)
-- ============================================
PRINT '2. Deploying Items Procedures...';
:r procedures\items\usp_add_item_to_workspace.sql
:r procedures\items\usp_move_item.sql
:r procedures\items\usp_remove_item_from_workspace.sql
:r procedures\items\usp_get_workspace_tree.sql
:r procedures\items\usp_get_item_children.sql
:r procedures\items\usp_get_item_path_info.sql
:r procedures\items\usp_bulk_add_items_v2.sql
:r procedures\items\usp_bulk_add_items.sql
:r procedures\items\usp_reorder_items.sql
:r procedures\items\usp_bulk_move_items.sql
PRINT '';

-- ============================================
-- 3. NOTES PROCEDURES (14)
-- ============================================
PRINT '3. Deploying Notes Procedures...';
:r procedures\notes\usp_create_note.sql
:r procedures\notes\usp_get_note.sql
:r procedures\notes\usp_update_note.sql
:r procedures\notes\usp_delete_note.sql
:r procedures\notes\usp_restore_note.sql
:r procedures\notes\usp_get_note_versions.sql
:r procedures\notes\usp_get_note_version.sql
:r procedures\notes\usp_restore_note_version.sql
:r procedures\notes\usp_share_note.sql
:r procedures\notes\usp_change_note_member_role.sql
:r procedures\notes\usp_unshare_note.sql
:r procedures\notes\usp_get_note_members.sql
:r procedures\notes\usp_get_user_notes.sql
:r procedures\notes\usp_search_notes.sql
PRINT '';

-- ============================================
-- 4. CACHE PROCEDURES (10)
-- ============================================
PRINT '4. Deploying Cache Procedures...';
:r procedures\cache\usp_refresh_workspace_tree_cache.sql
:r procedures\cache\usp_refresh_all_workspace_caches.sql
:r procedures\cache\usp_get_workspace_tree_cached.sql
:r procedures\cache\usp_get_subtree_cached.sql
:r procedures\cache\usp_get_children_cached.sql
:r procedures\cache\usp_get_breadcrumb_cached.sql
:r procedures\cache\usp_invalidate_workspace_cache.sql
:r procedures\cache\usp_cleanup_old_caches.sql
:r procedures\cache\usp_get_cache_statistics.sql
:r procedures\cache\usp_compare_query_performance.sql
PRINT '';

-- ============================================
-- 5. AUDIT PROCEDURES (6)
-- ============================================
PRINT '5. Deploying Audit Procedures...';
:r procedures\audit\usp_get_audit_history.sql
:r procedures\audit\usp_get_security_events.sql
:r procedures\audit\usp_get_user_activity.sql
:r procedures\audit\usp_get_workspace_audit_trail.sql
:r procedures\audit\usp_cleanup_old_audit_logs.sql
:r procedures\audit\usp_test_audit_triggers.sql
PRINT '';

-- ============================================
-- 6. MAINTENANCE PROCEDURES (9)
-- ============================================
PRINT '6. Deploying Maintenance Procedures...';
:r procedures\maintenance\usp_rebuild_fragmented_indexes.sql
:r procedures\maintenance\usp_update_all_statistics.sql
:r procedures\maintenance\usp_s_index_usage_stats.sql
:r procedures\maintenance\usp_s_missing_indexes.sql
:r procedures\maintenance\usp_s_index_fragmentation.sql
:r procedures\maintenance\usp_s_slow_queries.sql
:r procedures\maintenance\usp_s_table_sizes.sql
:r procedures\maintenance\usp_validate_data_integrity.sql
:r procedures\maintenance\usp_fix_data_issues.sql
PRINT '';

-- ============================================
-- DEPLOYMENT SUMMARY
-- ============================================
PRINT '============================================';
PRINT 'Procedure Deployment Complete!';
PRINT '============================================';
PRINT '';
PRINT 'Summary:';
PRINT '  ✅ Workspace Procedures: 12';
PRINT '  ✅ Items Procedures: 10';
PRINT '  ✅ Notes Procedures: 14';
PRINT '  ✅ Cache Procedures: 10';
PRINT '  ✅ Audit Procedures: 6';
PRINT '  ✅ Maintenance Procedures: 9';
PRINT '  ✅ Total: 61 procedures';
PRINT '';
PRINT 'Run this script with:';
PRINT '  sqlcmd -S servername -d database -i 00-RUN-ALL-PROCEDURES.sql';
PRINT '';
PRINT '============================================';
GO
