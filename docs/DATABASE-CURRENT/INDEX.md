# SuperApp Database - CURRENT IMPLEMENTATION

**Version:** MVP 1.0 (Deployed)
**Last Updated:** October 15, 2025
**Status:** ✅ Production Ready

---

## 📋 Quick Links

- **[ERD Diagram](ERD-DIAGRAM.md)** - Visual schema with all fields and relationships
- **[ERD Simple](ERD-SIMPLE.md)** - Simplified overview (quick reference)
- **[Full Design Documentation](../DATABASE/INDEX.md)** - Complete design with future features
- **[Implementation Status](#implementation-status)** - What's deployed vs planned
- **[Table Reference](#table-reference)** - Direct links to table definitions

---

## 🎯 Purpose

This folder contains **100% accurate documentation of the CURRENTLY DEPLOYED database** (MVP). It reflects the actual production schema, not future plans.

**For future features and design references:** See [`DATABASE/`](../DATABASE/) folder.

---

## 📊 Overview

**Current MVP supports:**
- ✅ User authentication and profiles
- ✅ Global tag management (per user)
- ✅ Workspace organization (hierarchy mode)
- ✅ Workspace sharing (3 roles: owner/editor/viewer)
- ✅ Notes with markdown content
- ✅ Note versioning (auto-created on changes)
- ✅ Note sharing and collaboration

**Scale (Current):**
- 5 test users
- 9 tags
- 5 workspaces
- 12 notes with versions
- ~80 total rows

---

## 🗂️ Database Structure (MVP)

### Entity Relationship Diagram (ACTUAL)

```
┌─────────────┐
│   users     │  (5 rows)
│             │
└──────┬──────┘
       │ 1
       │
       ├──────────────────────────────────┐
       │                                  │
       │ N                                │ N
┌──────▼──────────┐         ┌────────────▼─────────┐
│   workspaces    │────────►│ workspace_members    │
│   (5 rows)      │  1   N  │ (17 rows)            │
└──────┬──────────┘         └──────────────────────┘
       │ 1
       │
       │ N
┌──────▼────────────────────────┐
│ workspace_relationship_types  │ (5 rows)
│ (custom relationship defs)    │
└───────────────────────────────┘

┌─────────────┐
│    tags     │  (9 rows)
│ (global)    │
└──────┬──────┘
       │ N
       │
       │ N    ⚠️ UNIFIED TABLE
┌──────▼─────────────────────┐
│ workspace_items            │ (0 rows - ready to use)
│ Handles BOTH:              │
│  - Tag → Tag links         │
│  - Tag → Note links        │
└────────────────────────────┘

┌─────────────┐
│   notes     │  (12 rows)
└──────┬──────┘
       │ 1
       │
       ├──────────────┬────────────────┐
       │ N            │ N              │ N
┌──────▼──────┐  ┌───▼────────┐  ┌────▼──────────┐
│note_members │  │note_versions│  │entity_types   │
│(12 rows)    │  │(12 rows)    │  │(3 types)      │
└─────────────┘  └─────────────┘  └───────────────┘
```

### ⚠️ Key Design Difference: UNIFIED workspace_items

**Original Design ([`DATABASE/INDEX.md`](../DATABASE/INDEX.md)):**
- `workspace_tag_relationships` - Tag-to-Tag only
- `workspace_items` - Tag-to-Note only

**ACTUAL Implementation (MVP):**
- ❌ `workspace_tag_relationships` - NOT deployed
- ✅ `workspace_items` - **UNIFIED table** handling BOTH tag-tag AND tag-note

**Why UNIFIED?**
1. ✅ Simpler (1 table instead of 2)
2. ✅ Fewer joins
3. ✅ Sufficient for MVP scale
4. ⏳ Can split into 2 tables later if needed (Phase 2)

---

## 📈 Implementation Status

### ✅ Deployed Tables (10)

| # | Table | Rows | Purpose | File |
|---|-------|------|---------|------|
| 1 | `users` | 5 | User accounts | [tables/core/users.sql](tables/core/users.sql) |
| 2 | `tags` | 9 | Global tag pool | [tables/core/tags.sql](tables/core/tags.sql) |
| 3 | `entity_types` | 3 | Entity type registry | [tables/core/entity_types.sql](tables/core/entity_types.sql) |
| 4 | `workspaces` | 5 | Workspace containers | [tables/workspace/workspaces.sql](tables/workspace/workspaces.sql) |
| 5 | `workspace_members` | 17 | Workspace sharing | [tables/workspace/workspace_members.sql](tables/workspace/workspace_members.sql) |
| 6 | `workspace_relationship_types` | 5 | Relationship defs | [tables/workspace/workspace_relationship_types.sql](tables/workspace/workspace_relationship_types.sql) |
| 7 | `workspace_items` | 0 | UNIFIED items (tag+note) | [tables/entities/workspace_items.sql](tables/entities/workspace_items.sql) |
| 8 | `notes` | 12 | Markdown notes | [tables/entities/notes.sql](tables/entities/notes.sql) |
| 9 | `note_members` | 12 | Note sharing | [tables/entities/note_members.sql](tables/entities/note_members.sql) |
| 10 | `note_versions` | 12 | Version history | [tables/entities/note_versions.sql](tables/entities/note_versions.sql) |

### ✅ Deployed Procedures (15)

| Category | Count | Procedures |
|----------|-------|------------|
| **Workspace** | 6 | `usp_i_workspace`, `usp_s_workspace`, `usp_s_user_workspaces`, `usp_u_workspace`, `usp_d_workspace`, `usp_add_workspace_member` |
| **Items** | 5 | `usp_add_item_to_workspace`, `usp_s_workspace_items`, `usp_move_item`, `usp_remove_item`, `usp_s_item_path` |
| **Tags** | 4 | `usp_i_tag`, `usp_s_tag`, `usp_u_tag`, `usp_s_user_tags` |

**Files:**
- [procedures/workspace/workspace-procedures.sql](procedures/workspace/workspace-procedures.sql) - 6 workspace procedures
- [procedures/items/items-procedures.sql](procedures/items/items-procedures.sql) - 5 items procedures
- [procedures/tags/tags-procedures.sql](procedures/tags/tags-procedures.sql) - 4 tag procedures

### ✅ Deployed Triggers (8)

| Table | Trigger | Purpose |
|-------|---------|---------|
| **tags** (1) | `tr_tags_generate_slug` | Auto-generate slug from name |
| **workspaces** (3) | `tr_workspace_generate_slug` | Generate workspace slug |
| | `tr_workspaces_add_owner` | Auto-add creator as owner |
| | `tr_workspaces_update_stats` | Update tag/member counts |
| **workspace_items** (1) | `tr_workspace_items_update_depth` | Update hierarchy depth |
| **notes** (4) | `tr_notes_generate_slug` | Auto-generate slug (with recursion check) |
| | `tr_notes_updated_at` | Update timestamp (with recursion check) |
| | `tr_notes_add_owner` | Auto-add creator as owner |
| | `tr_notes_create_version` | Create version snapshot (with recursion check) |

**Note:** Notes triggers include `TRIGGER_NESTLEVEL()` checks to prevent recursion.

**Files:**
- [triggers/tags-triggers.sql](triggers/tags-triggers.sql) - 1 tag trigger
- [triggers/workspaces-triggers.sql](triggers/workspaces-triggers.sql) - Workspace triggers (2 deployed)
- [triggers/workspace-items-triggers.sql](triggers/workspace-items-triggers.sql) - 1 items trigger
- [triggers/notes-triggers.sql](triggers/notes-triggers.sql) - 4 note triggers (with recursion protection)

### ⏳ NOT Yet Deployed

| Feature | Status | Reason |
|---------|--------|--------|
| `audit_logs` table | ❌ Not needed yet | MVP doesn't require audit |
| Materialized views | ❌ Performance OK | <100 rows, no caching needed |
| `workspace_tag_relationships` | ❌ Using UNIFIED | Using `workspace_items` instead |
| Full-text search indexes | ❌ Small dataset | 12 notes, not needed yet |
| Cache refresh procedures | ❌ No cache tables | No materialized views |

---

## 📁 Table Reference

### Core Tables

**Location:** `tables/core/`

- **[users.sql](tables/core/users.sql)** - User accounts and authentication
  - Primary key: `user_id`
  - 3 indexes (email, username, active)
  - Soft delete: `deleted_at`

- **[tags.sql](tables/core/tags.sql)** - User-created tags
  - Primary key: `tag_id`
  - Foreign key: `user_id` → `users`
  - 3 indexes (user, name, usage)
  - Unique: `(user_id, slug)`
  - Trigger: Auto-generate slug

- **[entity_types.sql](tables/core/entity_types.sql)** - Entity type registry
  - Primary key: `type_name`
  - Seeded with 5 types (tag, note, project, document, task)
  - Only 'tag' and 'note' enabled in MVP

### Workspace Tables

**Location:** `tables/workspace/`

- **[workspaces.sql](tables/workspace/workspaces.sql)** - Workspace containers
  - Primary key: `workspace_id`
  - Foreign key: `user_id` → `users`
  - 3 indexes (user, type, recent)
  - 3 triggers (add_owner, init_types, update_stats)

- **[workspace_members.sql](tables/workspace/workspace_members.sql)** - Sharing & permissions
  - Primary key: `member_id`
  - Foreign keys: `workspace_id` → `workspaces`, `user_id` → `users`
  - 2 indexes (workspace, user)
  - Roles: owner/editor/viewer

- **[workspace_relationship_types.sql](tables/workspace/workspace_relationship_types.sql)** - Relationship definitions
  - Primary key: `relationship_type_id`
  - Foreign key: `workspace_id` → `workspaces`
  - 1 index (workspace)
  - Auto-created by workspace trigger

### Entity Tables

**Location:** `tables/entities/`

- **[workspace_items.sql](tables/entities/workspace_items.sql)** - ⚠️ UNIFIED table
  - Primary key: `item_id`
  - Foreign keys: `workspace_id`, `parent_tag_id`, `child_type`
  - 5 indexes (workspace, parent, child, path, roots)
  - Handles BOTH tag-tag and tag-note relationships
  - Unique: `(workspace_id, parent_tag_id, child_type, child_id)`

- **[notes.sql](tables/entities/notes.sql)** - Markdown notes
  - Primary key: `note_id`
  - Foreign key: `user_id` → `users`
  - 6 indexes (user, name, slug, archived, pinned, favorite)
  - 4 triggers (slug, updated_at, add_owner, create_version)

- **[note_members.sql](tables/entities/note_members.sql)** - Note sharing
  - Primary key: `member_id`
  - Foreign keys: `note_id` → `notes`, `user_id` → `users`
  - 3 indexes (note, user, owner)
  - Roles: owner/editor/viewer

- **[note_versions.sql](tables/entities/note_versions.sql)** - Version history
  - Primary key: `version_id`
  - Foreign keys: `note_id` → `notes`, `created_by` → `users`
  - 2 indexes (note, user)
  - Immutable (INSERT only)

---

## 🔧 Known Issues & Fixes

### ✅ Fixed Issues

1. **Foreign key references** - All fixed to use `user_id` (not `id`)
2. **Trigger recursion** - Notes triggers use `TRIGGER_NESTLEVEL()` checks
3. **Workspace slug column** - Removed (not in actual schema)
4. **Tags slug nullable** - Trigger generates after INSERT

**Details:** See `../DATABASE/VERIFICATION/FIXES-SUMMARY.md`

---

## 📚 Related Documentation

| Document | Purpose | Location |
|----------|---------|----------|
| **Full Design** | Complete design with future features | [`DATABASE/INDEX.md`](../DATABASE/INDEX.md) |
| **Status Report** | Deployment verification | [`DATABASE/VERIFICATION/DATABASE-COMPLETE-STATUS.md`](../DATABASE/VERIFICATION/DATABASE-COMPLETE-STATUS.md) |
| **Notes Feature** | Notes implementation details | [`DATABASE/VERIFICATION/NOTES-FEATURE-SUMMARY.md`](../DATABASE/VERIFICATION/NOTES-FEATURE-SUMMARY.md) |
| **Context** | Session context for AI | [`DATABASE/VERIFICATION/CONTEXT-FOR-NEXT-SESSION.md`](../DATABASE/VERIFICATION/CONTEXT-FOR-NEXT-SESSION.md) |
| **Test Data** | Sample data for testing | `DATABASE/DUMP-DATA-WITH-NOTES.sql` |

---

## 💡 For AI Reading

**Optimized structure for token efficiency:**

1. **Start here:** `INDEX.md` (this file) - Overview and implementation status
2. **Table details:** `tables/**/*.sql` - Individual table definitions
3. **Design reference:** `../DATABASE/INDEX.md` - Full design with future features
4. **Status details:** `../DATABASE/VERIFICATION/*.md` - Deployment verification

**Key facts:**
- 10 tables deployed (not 13 from design)
- `workspace_items` is UNIFIED (handles both tag-tag and tag-note)
- All foreign keys use correct column names (`user_id`, not `id`)
- Notes triggers have recursion protection
- 0 rows in `workspace_items` (ready for use, but no data yet)

---

**Last verified:** October 15, 2025
**Database:** SuperApp-dev (SQL Server)
**Deployment:** Complete ✅
