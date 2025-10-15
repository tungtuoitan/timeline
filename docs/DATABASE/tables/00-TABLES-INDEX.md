# SuperApp Database Tables - Complete Index

**Version:** 3.0 (Unified System)  
**Last Updated:** October 15, 2025  
**Purpose:** Modular table definitions organized by category

---

## 📋 Quick Navigation

- [**00-RUN-ALL-TABLES.sql**](./00-RUN-ALL-TABLES.sql) - Execute all tables in correct order
- [Core Tables](#core-tables) - Users & Tags (2 tables)
- [Workspace Tables](#workspace-tables) - Workspaces & Members (3 tables)
- [Entity Tables](#entity-tables) - Unified System & Notes (5 tables)
- [Indexes](#indexes) - Performance optimizations
- [Constraints](#constraints) - Data validation
- [Audit System](#audit-system) - Change tracking
- [Cache System](#cache-system) - Performance cache

---

## 🎯 Quick Start

### Execute All Tables
```sql
-- Run from docs/DB/tables/ directory
:r 00-RUN-ALL-TABLES.sql
```

### Execute Individual Categories
```sql
-- Core tables
:r core\users.sql
:r core\tags.sql

-- Workspace tables
:r workspace\workspaces.sql
:r workspace\workspace_members.sql
:r workspace\workspace_relationship_types.sql

-- Entity tables
:r entities\entity_types.sql
:r entities\workspace_items.sql
:r entities\notes.sql
:r entities\note_versions.sql
:r entities\note_members.sql
```

---

## 📦 Core Tables

Located in: `core/`

### 1. users.sql
- **Purpose:** User accounts
- **Scale:** 1,000+ users
- **Features:** 
  - Authentication (email, username, password_hash)
  - Profile (display_name, avatar_url, bio)
  - Settings (preferences JSON)
  - Soft delete support
- **Dependencies:** None
- **Indexes:** 3 (email, username, active users)
- **Triggers:** 1 (updated_at)
- **Views:** 1 (vw_active_users)

### 2. tags.sql
- **Purpose:** Global tag pool (shared across workspaces)
- **Scale:** 2,000+ tags per user, 2M+ total
- **Features:**
  - Tag information (name, slug, color, icon)
  - Usage statistics (usage_count)
  - Metadata JSON support
  - Auto-generated slugs
  - Soft delete support
- **Dependencies:** users.sql
- **Indexes:** 5 (user, name, slug, usage)
- **Triggers:** 2 (slug generation, updated_at)
- **Views:** 1 (vw_active_tags)

---

## 🏢 Workspace Tables

Located in: `workspace/`

### 3. workspaces.sql
- **Purpose:** Workspace containers for tag contexts
- **Scale:** 10-20 per user, 20,000+ total
- **Features:**
  - Workspace types (hierarchy, graph, network, list, kanban, custom)
  - Visual properties (color, icon)
  - Statistics (tag_count, item_count, member_count)
  - Template support
  - Soft delete support
- **Dependencies:** users.sql
- **Indexes:** 5 (user, type, recent access, templates, default)
- **Triggers:** 3 (add owner, init relationship types, updated_at)

### 4. workspace_members.sql
- **Purpose:** Workspace sharing with role-based permissions
- **Scale:** ~5 members per workspace, 100,000+ total
- **Features:**
  - Three roles: owner, editor, viewer
  - Invitation system (pending, active, declined, removed)
  - Member statistics
  - Soft delete support
- **Dependencies:** workspaces.sql, users.sql
- **Indexes:** 4 (workspace, user, owners, pending invitations)
- **Triggers:** 1 (update member count)

### 5. workspace_relationship_types.sql
- **Purpose:** Define available relationship types per workspace
- **Scale:** ~5 types per workspace, 100+ total
- **Features:**
  - Custom relationship types
  - Default types (parent-child, related-to, references, contains, linked-to)
  - Visual properties (color, icon)
  - Hierarchy configuration
  - Soft delete support
- **Dependencies:** workspaces.sql
- **Indexes:** 2 (workspace, type name)

---

## 📊 Entity Tables (Unified System)

Located in: `entities/`

### 6. entity_types.sql
- **Purpose:** Registry of supported entity types
- **Scale:** ~10 types (static reference data)
- **Features:**
  - Entity type definitions (tag, note, document, link, task)
  - Display properties
  - Capability flags (supports_versions, supports_sharing)
  - Icon mapping
- **Dependencies:** None
- **Indexes:** 1 (active types)
- **Data:** Pre-populated with default types

### 7. workspace_items.sql ⭐ **UNIFIED SYSTEM**
- **Purpose:** Unified table for ALL workspace contents (tag-to-tag AND entity-to-tag)
- **Scale:** 500-1,500 per workspace, 15M+ total
- **Features:**
  - Replaces workspace_tag_relationships + entity_tags
  - Hierarchical path tracking
  - Relationship types support
  - Depth calculations
  - Sort ordering
  - Soft delete support
- **Dependencies:** workspaces.sql, tags.sql, entity_types.sql
- **Indexes:** 6 (workspace, parent, child, path, roots, rel_type)
- **Triggers:** 3 (validate type, prevent cycles, update stats)

### 8. notes.sql
- **Purpose:** Markdown notes with rich metadata
- **Scale:** 2,000+ per user, 2M+ total
- **Features:**
  - Markdown content support
  - Note types (brainstorm, meeting, research, tutorial, reference)
  - Status flags (archived, pinned, favorite)
  - Version tracking
  - Statistics (word_count, view_count)
  - Soft delete support
- **Dependencies:** users.sql
- **Indexes:** 6 (user, name, slug, status filters)
- **Triggers:** 2 (slug generation, updated_at)

### 9. note_versions.sql
- **Purpose:** Version history for notes
- **Scale:** ~10 versions per note
- **Features:**
  - Full content snapshots
  - Change tracking
  - Version metadata
  - Soft delete support
- **Dependencies:** notes.sql, users.sql
- **Indexes:** 3 (note, creation time, created_by)

### 10. note_members.sql
- **Purpose:** Note sharing with role-based permissions
- **Scale:** ~3 members per shared note
- **Features:**
  - Three roles: owner, editor, viewer
  - Role capabilities (can_edit, can_delete, can_share)
  - Access tracking
  - Soft delete support
- **Dependencies:** notes.sql, users.sql
- **Indexes:** 3 (note, user, role)
- **Triggers:** 2 (validate permissions, update member_count)

---

## 🔍 Indexes

Located in: `indexes/`

Additional performance indexes beyond those in table definitions:

### composite_indexes.sql
- User login lookup
- Tag search (autocomplete)
- Workspace user recent access
- Permission checks

### covering_indexes.sql
- Tag display properties
- Workspace summaries
- Avoid key lookups

### workspace_items_indexes.sql
- Hierarchy queries
- Path-based searches
- Depth filtering
- Relationship type filtering

### notes_indexes.sql
- Note search
- Member lookups
- Version history

---

## 🔒 Constraints

Located in: `constraints/`

Data validation and business rules:

### user_constraints.sql
- Email format validation
- Username format
- Password hash format
- Display name length

### tag_constraints.sql
- Tag name validation
- Color format (hex)
- Slug format
- Usage count non-negative

### workspace_constraints.sql
- Workspace name validation
- Color format
- Statistics validation
- Member count validation

### workspace_items_constraints.sql
- Child type validation
- Depth constraints
- Path format validation
- Relationship type validation

### notes_constraints.sql
- Note name validation
- Content length
- Version constraints
- Member role validation

---

## 📝 Audit System

Located in: `audit/`

Change tracking and compliance:

### audit_log_table.sql
- Audit log table definition
- Change tracking structure

### workspace_member_audit.sql
- Role changes tracking
- Member addition/removal
- Permission changes

### note_sharing_audit.sql
- Note sharing events
- Permission changes
- Access tracking

### item_movement_audit.sql
- Item hierarchy changes
- Item deletion tracking

### audit_procedures.sql
- usp_get_audit_history
- usp_get_security_events
- usp_get_user_activity
- usp_get_workspace_audit_trail
- usp_cleanup_old_audit_logs

---

## ⚡ Cache System

Located in: `cache/`

Performance optimization for large workspaces:

### workspace_tree_cache.sql
- Materialized view table
- Pre-computed hierarchy trees
- For workspaces with 500+ items

### cache_refresh_procedures.sql
- usp_refresh_workspace_tree_cache
- usp_refresh_all_workspace_caches
- Force refresh options

### cache_query_procedures.sql
- usp_get_workspace_tree_cached (fast!)
- usp_get_subtree_cached
- usp_get_children_cached
- usp_get_breadcrumb_cached

### cache_maintenance.sql
- usp_invalidate_workspace_cache
- usp_cleanup_old_caches
- usp_get_cache_statistics

### cache_triggers.sql
- Auto-invalidation on changes
- Performance comparison tools

---

## 📊 Statistics

### Table Count: 12 tables
- Core: 2 tables
- Workspace: 3 tables
- Entities: 5 tables
- Audit: 1 table
- Cache: 1 table

### Index Count: 50+ indexes
- Filtered indexes for soft delete
- Covering indexes for performance
- Full-text search support

### Trigger Count: 15+ triggers
- Auto-update timestamps
- Data validation
- Statistics updates
- Cache invalidation
- Audit logging

### View Count: 5+ views
- Active users
- Active tags
- Workspace relationships
- Root tags
- Note summaries

### Stored Procedures: 30+ procedures
- Workspace operations (12)
- Item operations (10)
- Note operations (14)
- Audit queries (5)
- Cache management (5)

---

## 🎯 Execution Order

```
1. Core Tables
   ├── users.sql
   └── tags.sql

2. Workspace Tables
   ├── workspaces.sql
   ├── workspace_members.sql
   └── workspace_relationship_types.sql

3. Entity Tables
   ├── entity_types.sql
   ├── workspace_items.sql ⭐ UNIFIED
   ├── notes.sql
   ├── note_versions.sql
   └── note_members.sql

4. Additional Indexes
   ├── composite_indexes.sql
   ├── covering_indexes.sql
   ├── workspace_items_indexes.sql
   └── notes_indexes.sql

5. Constraints
   ├── user_constraints.sql
   ├── tag_constraints.sql
   ├── workspace_constraints.sql
   ├── workspace_items_constraints.sql
   └── notes_constraints.sql

6. Audit System
   ├── audit_log_table.sql
   ├── workspace_member_audit.sql
   ├── note_sharing_audit.sql
   ├── item_movement_audit.sql
   └── audit_procedures.sql

7. Cache System
   ├── workspace_tree_cache.sql
   ├── cache_refresh_procedures.sql
   ├── cache_query_procedures.sql
   ├── cache_maintenance.sql
   └── cache_triggers.sql
```

---

## 📖 Related Documentation

- [../01-overview.md](../01-overview.md) - Complete documentation index
- [../architecture-decisions.md](../architecture-decisions.md) - Design rationale
- [../schema-design.md](../schema-design.md) - ERD and query patterns
- [../performance-enssentials.md](../performance-enssentials.md) - Optimization guide
- [../procedures/00-PROCEDURES-INDEX.md](../procedures/00-PROCEDURES-INDEX.md) - Stored procedures

---

## ✅ Verification

After running all scripts:

```sql
-- Verify tables
SELECT name, create_date FROM sys.tables 
WHERE name IN (
    'users', 'tags', 'workspaces', 'workspace_members',
    'workspace_relationship_types', 'entity_types', 'workspace_items',
    'notes', 'note_versions', 'note_members', 'audit_log', 'workspace_tree_cache'
)
ORDER BY name;

-- Verify indexes
SELECT t.name AS table_name, i.name AS index_name, i.type_desc
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name LIKE 'workspace%' OR t.name IN ('users', 'tags', 'notes')
ORDER BY t.name, i.name;

-- Verify constraints
SELECT t.name AS table_name, c.name AS constraint_name, c.type_desc
FROM sys.objects c
INNER JOIN sys.tables t ON c.parent_object_id = t.object_id
WHERE t.name IN ('users', 'tags', 'workspaces', 'workspace_items', 'notes')
ORDER BY t.name, c.type_desc;
```

---

**✨ Modular structure allows:**
- Easy maintenance
- Selective execution
- Clear dependencies
- Version control friendly
- Better documentation
