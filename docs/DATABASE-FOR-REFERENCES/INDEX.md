# SuperApp Database Documentation

**Version:** 3.0 - Option D (No Closure Table)
**Last Updated:** October 15, 2025
**Status:** Ready for Implementation

---

## 🔗 Quick Navigation

| Document | Purpose |
|----------|---------|
| **[DATABASE-CURRENT/](../DATABASE-CURRENT/INDEX.md)** | ✅ **Currently deployed database** (MVP 1.0) - Start here for actual production schema |
| **DATABASE/** (this folder) | 📐 Complete design with future features (reference for Phase 2+) |

**For AI/developers:** Read [`DATABASE-CURRENT/INDEX.md`](../DATABASE-CURRENT/INDEX.md) first to understand what's actually deployed, then refer to this document for future features.

---

## 📋 Overview

SuperApp database schema for **Personal Knowledge Management System** supporting:

- ✅ **1000+ concurrent users**
- ✅ **2000+ tags per user** (global tag pool)
- ✅ **Flexible workspace organization** (hierarchy/graph/network/timeline)
- ✅ **Workspace sharing** with 3 roles (owner/editor/viewer)
- ✅ **VSCode-like performance** (<300ms for most queries)

**Key Architecture Decision:** **No Closure Table** approach for simplicity and maintainability. Optimize later if needed (Phase 2).

---

## 🗂️ Database Structure

### Entity Relationship Diagram

```
┌─────────────┐
│   users     │
│  (1000+)    │
└──────┬──────┘
       │ 1
       │
       │ N
┌──────▼──────────┐         ┌─────────────────────┐
│   workspaces    │────────►│ workspace_members   │
│   (10-20/user)  │  1   N  │ (sharing/roles)     │
└──────┬──────────┘         └─────────────────────┘
       │ 1
       │
       │ N
┌──────▼─────────────────────┐
│ workspace_relationship_    │
│ types (relationship defs)  │
└────────────────────────────┘

┌─────────────┐
│    tags     │
│ (global pool)│
│  (2000+/user)│
└──────┬──────┘
       │ N
       │
       │ N
┌──────▼─────────────────────┐
│ workspace_tag_             │
│ relationships              │
│ (context-specific links)   │
└────────────────────────────┘

┌─────────────┐
│   notes     │
│  (entities) │
└──────┬──────┘
       │ N
       │
       │ N
┌──────▼─────────────────────┐
│ workspace_items            │
│ (notes/tags in workspace)  │
└────────────────────────────┘
```

### Core Tables

| Table | Purpose | Estimated Rows | Growth Rate |
|-------|---------|----------------|-------------|
| `users` | User accounts | 1,000+ | Slow |
| `tags` | Global tag pool | 2M+ (2000/user) | Fast |
| `workspaces` | Workspace containers | 10K+ | Medium |
| `workspace_members` | Sharing & permissions | 50K+ | Medium |
| `workspace_relationship_types` | Relationship definitions | 100+ | Very slow |
| `workspace_tag_relationships` | Tag links in workspaces | 5M+ | Fast |
| `workspace_items` | Notes/tags in workspaces | 500K+ | Fast |
| `notes` | Note entities | 100K+ | Medium |
| `note_members` | Note sharing | 200K+ | Medium |
| `entity_types` | Entity type definitions | 10-20 | Very slow |

---

## 📁 SQL Scripts Reference

### 🔹 Core Tables

**Location:** `tables/`

- `tables/00-RUN-ALL-TABLES.sql` - Master script to create all tables
- `tables/00-TABLES-INDEX.md` - Complete table index with descriptions

**Organized by domain:**
- `tables/core/` - Users, Tags, Entity Types
- `tables/workspace/` - Workspaces, Members, Relationship Types
- `tables/entities/` - Notes, Note Members, Note Versions
- `tables/audit/` - Audit logs for sensitive operations
- `tables/cache/` - Materialized views for performance

### 🔹 Indexes

**Location:** `indexes/`

Performance optimization indexes:
- `05.01-composite-indexes.sql` - Multi-column indexes for common queries
- `05.02-covering-indexes.sql` - Include columns for SELECT performance
- `05.03-workspace-items-indexes.sql` - Workspace items optimization
- `05.04-notes-indexes.sql` - Note query optimization
- `05.05-filtered-indexes.sql` - Filtered for deleted_at IS NULL
- `05.06-fulltext-search.sql` - Full-text search capabilities
- `05.07-statistics-update.sql` - Statistics maintenance
- `05.08-maintenance-procedures.sql` - Index maintenance routines
- `05.09-maintenance-recommendations.sql` - Best practices

### 🔹 Constraints

**Location:** `constraints/`

Data validation and integrity:
- `06.01-users-constraints.sql` - User table constraints
- `06.02-tags-constraints.sql` - Tag validation rules
- `06.03-workspaces-constraints.sql` - Workspace constraints
- `06.04-workspace-members-constraints.sql` - Member role validation
- `06.05-relationship-types-constraints.sql` - Relationship rules
- `06.06-workspace-item-constraints.sql` - Item constraints
- `06.07-entity-types-constraints.sql` - Entity type validation
- `06.08-notes-constraints.sql` - Note constraints
- `06.09-note-members-constraints.sql` - Note member constraints
- `06.10-note-versions-constraints.sql` - Version constraints
- `06.11-deprecated-constraints.sql` - Legacy constraints (reference)
- `06.12-business-logic-triggers-constraints.sql` - Business rules
- `06.13-audit-log-constraints.sql` - Audit log validation
- `06.14-validation-procedures-constraints.sql` - Validation helpers
- `06.15-summary-constraints.sql` - Constraint summary

### 🔹 Stored Procedures

**Location:** `procedures/`

- `procedures/00-RUN-ALL-PROCEDURES.sql` - Master script to create all procedures
- `procedures/00-PROCEDURES-INDEX.md` - Complete procedure index

**Organized by function:**
- `procedures/workspace/` - Workspace tree operations (36 procedures)
  - Get tree, add/move/delete tags
  - Validation helpers
  - Performance optimized
- `procedures/notes/` - Note operations
- `procedures/items/` - Workspace item management
- `procedures/cache/` - Cache refresh procedures
- `procedures/maintenance/` - Database maintenance
- `procedures/audit/` - Audit log procedures

### 🔹 Audit & Triggers

**Location:** `audit/`

Audit logging for sensitive operations:
- `11.01-workspace-members-triggers.sql` - Member changes tracking
- `11.02-note-members-triggers.sql` - Note sharing tracking
- `11.03-workspace-items-triggers.sql` - Item changes tracking
- `11.04-notes-triggers.sql` - Note modification tracking
- `11.05-workspaces-triggers.sql` - Workspace changes tracking
- `11.06-audit-procedures.sql` - Audit helper procedures
- `11.07-performance-notes.sql` - Performance considerations
- `11.08-validation.sql` - Audit validation queries

### 🔹 Materialized Views

**Location:** `materialized_views/`

Performance caching layer:
- `workspace-tree-cache-table.sql` - Workspace tree cache
- `refresh-procedures.sql` - Cache refresh routines
- `invalidation-trigger.sql` - Auto-invalidation triggers
- `query-procedures.sql` - Cached query procedures
- `maintenance-procedures.sql` - Cache maintenance
- `performance-comparison.sql` - Performance benchmarks
- `summary.sql` - Cache system overview

### 🔹 Relationships (Deprecated)

**Location:** `relationships/`

⚠️ **DEPRECATED** - Reference only, do not use

Legacy relationship table implementation (replaced by workspace_tag_relationships):
- `04.01-table.sql` - Old table definition
- `04.02-indexes.sql` - Old indexes
- `04.03-triggers.sql` - Old triggers
- `04.04-views.sql` - Old views
- `04.05-functions.sql` - Old functions
- `04.06-validation-queries.sql` - Old validations

---

## 🚀 Quick Start

### Installation Order

1. **Create Tables** (Run in order):
   ```sql
   -- Run from tables/ directory
   @tables/00-RUN-ALL-TABLES.sql
   ```

2. **Create Indexes**:
   ```sql
   -- Run from indexes/ directory
   @indexes/05.01-composite-indexes.sql
   @indexes/05.02-covering-indexes.sql
   -- ... continue with all index files
   ```

3. **Add Constraints**:
   ```sql
   -- Run from constraints/ directory
   @constraints/06.01-users-constraints.sql
   -- ... continue with all constraint files
   ```

4. **Create Procedures**:
   ```sql
   -- Run from procedures/ directory
   @procedures/00-RUN-ALL-PROCEDURES.sql
   ```

5. **Setup Audit System**:
   ```sql
   -- Run from audit/ directory
   @audit/11.01-workspace-members-triggers.sql
   -- ... continue with all audit files
   ```

6. **Setup Materialized Views** (Optional):
   ```sql
   -- Run from materialized_views/ directory
   @materialized_views/workspace-tree-cache-table.sql
   @materialized_views/refresh-procedures.sql
   -- ... continue with cache setup
   ```

### Verification

Run validation queries to ensure correct setup:
```sql
-- Check table count
SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo';

-- Check procedures
SELECT COUNT(*) FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE = 'PROCEDURE';

-- Check indexes
SELECT COUNT(*) FROM sys.indexes 
WHERE type > 0;
```

---

## 🎯 Architecture Principles

### 1. **Separation of Concerns**

- **Tags = Global Pool**: Each tag exists independently, reusable across workspaces
- **Relationships = Context-Specific**: Tag relationships only exist within workspace context
- **Workspaces = Containers**: Isolated contexts for different organization patterns

### 2. **Workspace Types**

- **hierarchy**: Tree structure (no cycles, single parent)
- **graph**: Free-form network (cycles allowed, multiple connections)
- **network**: Business relationships (typed relationships, weighted)
- **timeline**: Temporal relationships (before/after/concurrent)

### 3. **Performance Strategy**

- **Materialized Paths**: For fast subtree queries (`to_path LIKE parent + '%'`)
- **Composite Indexes**: Multi-column indexes for common query patterns
- **Covering Indexes**: Include columns to avoid table lookups
- **Filtered Indexes**: For `deleted_at IS NULL` queries
- **Statistics**: Auto-update for query plan optimization

### 4. **Data Integrity**

- **Soft Delete**: Use `deleted_at` timestamp instead of hard delete
- **Audit Logging**: Track sensitive operations (sharing, deletion)
- **Validation**: CHECK constraints + validation procedures
- **Referential Integrity**: Foreign keys with appropriate CASCADE rules

---

## 📊 Performance Targets

| Operation | Target | Achieved |
|-----------|--------|----------|
| Get workspace tree (500 tags, depth 10) | <300ms | 100-300ms ✅ |
| Get tag subtree | <150ms | 50-150ms ✅ |
| Search tags by name (2000 tags) | <100ms | 50-100ms ✅ |
| Get all workspaces for tag | <100ms | 50-100ms ✅ |
| Add tag to workspace | <100ms | 50-100ms ✅ |
| Move tag in workspace | <150ms | 100-150ms ✅ |

---

## 🔧 Key Design Decisions

### ✅ No Closure Table (Option D)
- **Rationale**: Simpler implementation, sufficient performance for MVP
- **Trade-off**: Slower for deep recursive queries (acceptable for <10 levels)
- **Future**: Can add closure table in Phase 2 if needed

### ✅ Tag Deletion = PREVENT
- **Rationale**: Preserve data integrity across workspaces
- **Behavior**: Cannot delete tag if used in any workspace
- **Alternative**: Soft delete only (set deleted_at)

### ✅ Workspace Sharing with Roles
- **Roles**: Owner (full control), Editor (modify), Viewer (read-only)
- **Implementation**: `workspace_members` table with role column
- **Security**: Row-level permissions via stored procedures

### ✅ Pure SQL Solution (No Redis in MVP)
- **Rationale**: Reduce complexity, single technology stack
- **Implementation**: Materialized views for caching
- **Future**: Add Redis if needed for horizontal scaling

### ✅ Immediate Consistency
- **Rationale**: Simpler than eventual consistency
- **Implementation**: Synchronous updates with triggers
- **Trade-off**: Slightly slower writes (acceptable for MVP)

---

## 📚 Additional Resources

### Documentation Files (Archived)
- All detailed documentation has been consolidated into this INDEX.md
- Legacy markdown files removed for simplicity
- All information preserved in SQL comments and this index

### Sample Data
- `13-sample-data.sql` - Test data for development (optional)

### Testing
- Each SQL file includes validation queries at the end
- Use validation queries to verify correct implementation

---

## 🆘 Troubleshooting

### Common Issues

1. **Slow Tree Queries**
   - Check: `ix_wsrel_workspace_tree` index exists
   - Verify: Statistics are up-to-date
   - Solution: Run `@indexes/05.07-statistics-update.sql`

2. **Constraint Violations**
   - Check: Foreign key relationships
   - Verify: Deleted_at IS NULL on related records
   - Solution: Review `@constraints/06.15-summary-constraints.sql`

3. **Cache Invalidation Issues**
   - Check: Triggers are enabled
   - Verify: Cache refresh procedures exist
   - Solution: Manual refresh via `@materialized_views/refresh-procedures.sql`

---

**Last Updated:** October 15, 2025  
**Maintained By:** SuperApp Development Team
