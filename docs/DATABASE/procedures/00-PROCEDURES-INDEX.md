# 📋 Stored Procedures Index

**Purpose**: Complete index of all stored procedures organized by domain  
**Last Updated**: October 14, 2025

---

## 📁 Folder Structure

```
procedures/
├── workspace/      # Workspace CRUD and member management
├── items/          # Workspace items operations
├── notes/          # Note CRUD and version control
├── cache/          # Performance caching procedures
├── audit/          # Audit logging and security
├── maintenance/    # Database maintenance and optimization
└── 00-RUN-ALL-PROCEDURES.sql    # Master script to create all procedures
```

---

## 📊 Workspace Procedures (12 total)

**Location**: `procedures/workspace/`

### CRUD Operations (5)
- `usp_i_workspace.sql` - Create new workspace
- `usp_s_workspace.sql` - Get workspace details
- `usp_s_workspaces.sql` - List user's workspaces
- `usp_u_workspace.sql` - Update workspace
- `usp_d_workspace.sql` - Delete workspace (soft delete)

### Member Management (3)
- `usp_add_workspace_member.sql` - Add member to workspace
- `usp_remove_workspace_member.sql` - Remove member from workspace
- `usp_change_workspace_member_role.sql` - Change member's role

### Queries & Utilities (4)
- `usp_s_workspace_stats.sql` - Get workspace statistics
- `usp_search_workspaces.sql` - Search workspaces by name
- `usp_update_workspace_access.sql` - Update last_accessed_at
- `usp_clone_workspace.sql` - Clone workspace structure

---

## 📦 Workspace Items Procedures (10 total)

**Location**: `procedures/items/`

### Basic Operations (3)
- `usp_add_item_to_workspace.sql` - Add tag/note to workspace
- `usp_move_item.sql` - Move item to different parent
- `usp_remove_item_from_workspace.sql` - Remove item from workspace

### Tree Queries (3)
- `usp_get_workspace_tree.sql` - Get complete workspace tree
- `usp_get_item_children.sql` - Get immediate children of item
- `usp_get_item_path_info.sql` - Get breadcrumb path for item

### Bulk Operations (4)
- `usp_bulk_add_items_v2.sql` - Bulk add items (latest version)
- `usp_bulk_add_items.sql` - Bulk add items (legacy)
- `usp_reorder_items.sql` - Reorder sibling items
- `usp_bulk_move_items.sql` - Move multiple items at once

---

## 📝 Note Procedures (14 total)

**Location**: `procedures/notes/`

### CRUD Operations (5)
- `usp_create_note.sql` - Create new note
- `usp_get_note.sql` - Get note by ID
- `usp_update_note.sql` - Update note
- `usp_delete_note.sql` - Delete note (soft delete)
- `usp_restore_note.sql` - Restore deleted note

### Version Management (4)
- `usp_get_note_versions.sql` - List note versions
- `usp_get_note_version.sql` - Get specific version
- `usp_restore_note_version.sql` - Restore from version
- (Auto-created by trigger: `tr_notes_create_version`)

### Sharing & Permissions (4)
- `usp_share_note.sql` - Share note with user
- `usp_change_note_member_role.sql` - Change member's role
- `usp_unshare_note.sql` - Remove member from note
- `usp_get_note_members.sql` - List note members

### Queries (2)
- `usp_get_user_notes.sql` - Get user's notes
- `usp_search_notes.sql` - Search notes by content

---

## 🚀 Cache Procedures (10 total)

**Location**: `procedures/cache/`

### Cache Management (3)
- `usp_refresh_workspace_tree_cache.sql` - Refresh single workspace cache
- `usp_refresh_all_workspace_caches.sql` - Refresh all workspace caches
- `usp_invalidate_workspace_cache.sql` - Clear workspace cache

### Cache Queries (4)
- `usp_get_workspace_tree_cached.sql` - Get cached tree
- `usp_get_subtree_cached.sql` - Get cached subtree
- `usp_get_children_cached.sql` - Get cached children
- `usp_get_breadcrumb_cached.sql` - Get cached breadcrumb

### Cache Maintenance (3)
- `usp_cleanup_old_caches.sql` - Remove old cache entries
- `usp_get_cache_statistics.sql` - Cache performance stats
- `usp_compare_query_performance.sql` - Compare cached vs non-cached

---

## 🔒 Audit Procedures (6 total)

**Location**: `procedures/audit/`

### Audit Queries (4)
- `usp_get_audit_history.sql` - Get audit history for entity
- `usp_get_security_events.sql` - Get security-related events
- `usp_get_user_activity.sql` - Get user activity log
- `usp_get_workspace_audit_trail.sql` - Get workspace audit trail

### Maintenance (2)
- `usp_cleanup_old_audit_logs.sql` - Clean up old audit logs
- `usp_test_audit_triggers.sql` - Test audit trigger functionality

---

## 🔧 Maintenance Procedures (9 total)

**Location**: `procedures/maintenance/`

### Data Integrity (2)
- `usp_validate_data_integrity.sql` - Check data integrity
- `usp_fix_data_issues.sql` - Fix common data issues

### Index Management (5)
- `usp_rebuild_fragmented_indexes.sql` - Rebuild fragmented indexes
- `usp_update_all_statistics.sql` - Update statistics
- `usp_s_index_usage_stats.sql` - Get index usage statistics
- `usp_s_missing_indexes.sql` - Find missing indexes
- `usp_s_index_fragmentation.sql` - Check index fragmentation

### Performance Monitoring (2)
- `usp_s_slow_queries.sql` - Find slow queries
- `usp_s_table_sizes.sql` - Get table sizes

---

## 🚀 Quick Start

### Option 1: Run All Procedures at Once
```sql
-- Execute master script to create all procedures
:r procedures/00-RUN-ALL-PROCEDURES.sql
```

### Option 2: Run by Domain
```sql
-- Workspace procedures only
:r procedures/workspace/usp_i_workspace.sql
:r procedures/workspace/usp_s_workspace.sql
-- ... etc

-- Items procedures only
:r procedures/items/usp_add_item_to_workspace.sql
-- ... etc
```

### Option 3: Run Individual Procedure
```sql
-- Single procedure
:r procedures/workspace/usp_i_workspace.sql
```

---

## 📈 Procedure Statistics

| Domain | Procedures | Purpose |
|--------|-----------|---------|
| **Workspace** | 12 | Workspace management & sharing |
| **Items** | 10 | Tree structure operations |
| **Notes** | 14 | Note CRUD & versioning |
| **Cache** | 10 | Performance optimization |
| **Audit** | 6 | Security & compliance |
| **Maintenance** | 9 | Database health |
| **TOTAL** | **61** | Complete system |

---

## 🔍 Finding Procedures

### By Function
```sql
-- Search procedure names
SELECT name 
FROM sys.procedures 
WHERE name LIKE '%workspace%'
ORDER BY name;

-- Search procedure text
SELECT OBJECT_NAME(object_id) AS procedure_name
FROM sys.sql_modules
WHERE definition LIKE '%workspace_items%'
ORDER BY procedure_name;
```

### By Domain
See folder structure above. Each domain has its own folder with related procedures.

---

## 📝 Naming Convention

All procedures follow the pattern: `usp_<action>_<entity>`

### Action Prefixes
- `i_` = Insert/Create
- `s_` = Select/Get
- `u_` = Update
- `d_` = Delete
- No prefix = Complex operation (e.g., `usp_add_item_to_workspace`)

### Examples
- `usp_i_workspace` - **I**nsert workspace
- `usp_s_workspace` - **S**elect workspace
- `usp_u_workspace` - **U**pdate workspace
- `usp_d_workspace` - **D**elete workspace
- `usp_clone_workspace` - Complex operation

---

## 🔗 Dependencies

### Execution Order
1. **Tables** (02-07) must exist first
2. **Procedures** can be created in any order (no dependencies between them)
3. **Triggers** (11) reference some procedures

### Required Objects
All procedures depend on:
- Core tables (users, tags, workspaces)
- Entity tables (workspace_items, notes)
- Audit table (audit_log)

---

## 📚 Documentation

Each procedure file contains:
- **Purpose**: What the procedure does
- **Parameters**: Input parameters with descriptions
- **Returns**: Result sets returned
- **Permissions**: Role requirements
- **Examples**: Usage examples
- **Notes**: Special considerations

---

## ✅ Migration Guide

### From Old Structure
```sql
-- Old: All procedures in one file
:r 08-procedures-workspace.sql  -- 928 lines ❌

-- New: Organized by domain
:r procedures/00-RUN-ALL-PROCEDURES.sql  -- ✅
-- OR run individual procedures as needed
```

### Benefits
- ✅ Easier to find specific procedures
- ✅ Better version control (smaller diffs)
- ✅ Easier to update individual procedures
- ✅ Clearer organization by domain
- ✅ Better team collaboration (less merge conflicts)

---

## 🆘 Troubleshooting

### Procedure Not Found
```sql
-- Check if procedure exists
SELECT * 
FROM sys.procedures 
WHERE name = 'usp_i_workspace';

-- Drop and recreate
DROP PROCEDURE IF EXISTS usp_i_workspace;
:r procedures/workspace/usp_i_workspace.sql
```

### Permission Issues
```sql
-- Grant execute permission
GRANT EXECUTE ON usp_i_workspace TO [YourRole];

-- Grant on schema
GRANT EXECUTE ON SCHEMA::dbo TO [YourRole];
```

### Performance Issues
```sql
-- Check procedure stats
:r procedures/maintenance/usp_s_slow_queries.sql
EXEC usp_s_slow_queries;

-- Update statistics
:r procedures/maintenance/usp_update_all_statistics.sql
EXEC usp_update_all_statistics;
```

---

**Last Updated**: October 14, 2025  
**Version**: 3.0  
**Status**: ✅ Ready for Production
