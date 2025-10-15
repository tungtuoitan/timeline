# DATABASE-FOR-REFERENCES

**Purpose:** Design reference and future feature documentation

---

## ⚠️ IMPORTANT

This folder contains **DESIGN REFERENCE ONLY** for future database features and enhancements.

**DO NOT use these files for deployment!**

---

## 📁 What's Here

### ✅ Design Reference Files

| Folder | Purpose |
|--------|---------|
| **INDEX.md** | Complete database design with all planned features |
| **tables/** | Table designs for reference (not deployed versions) |
| **procedures/** | Stored procedure designs for future features |
| **triggers/** | Trigger designs and patterns |

---

## 🚫 What's NOT Here (Removed)

The following have been removed as they were deployment/testing artifacts:

- ❌ `DUMP-DATA-*.sql` - Test data dumps
- ❌ `DEPLOY-MVP-*.sql` - Deployment scripts (moved to DATABASE-CURRENT)
- ❌ `VERIFICATION/` - Testing and verification files
- ❌ `KE_HOACH_TRIEN_KHAI_DATABASE.md` - Deployment plan
- ❌ `audit/`, `constraints/`, `indexes/`, `materialized_views/`, `relationships/` - Implementation details

---

## 🎯 When to Use This Folder

**Use this folder when:**
- Planning future features (Phase 2, 3, etc.)
- Researching advanced patterns (materialized views, audit logs, etc.)
- Understanding the complete vision of the database design

**DO NOT use this folder for:**
- ❌ Deployment - Use `DATABASE-CURRENT/` instead
- ❌ Understanding current schema - Use `DATABASE-CURRENT/` instead
- ❌ Writing application code - Use `DATABASE-CURRENT/` instead

---

## 📊 Current vs Future

| Aspect | DATABASE-CURRENT | DATABASE-FOR-REFERENCES |
|--------|------------------|------------------------|
| **Purpose** | Production schema | Design reference |
| **Content** | Actually deployed tables/procs | All planned features |
| **Accuracy** | 100% matches database | May differ from production |
| **Use for** | Development, deployment | Planning, research |
| **Status** | ✅ Production ready | 📋 Future roadmap |

---

## 🔗 Related Documentation

- **Current Implementation:** [`../DATABASE-CURRENT/INDEX.md`](../DATABASE-CURRENT/INDEX.md)
- **ERD Diagrams:** [`../DATABASE-CURRENT/ERD-DIAGRAM.md`](../DATABASE-CURRENT/ERD-DIAGRAM.md)
- **Deployment History:** See git history

---

## 📚 Key Design Files

### Main Index
- **[INDEX.md](INDEX.md)** - Complete database design documentation
  - All 13+ planned tables
  - Advanced features (audit logs, materialized views, full-text search)
  - Relationship patterns
  - Future enhancements

### Table Designs
- **[tables/core/](tables/core/)** - Core entity designs (users, tags, entity_types)
- **[tables/workspace/](tables/workspace/)** - Workspace system designs
- **[tables/entities/](tables/entities/)** - Entity management designs (notes, items)

### Procedure Designs
- **[procedures/workspace/](procedures/workspace/)** - Workspace management procedures
- **[procedures/items/](procedures/items/)** - Item hierarchy procedures
- **[procedures/tags/](procedures/tags/)** - Tag management procedures
- **[procedures/notes/](procedures/notes/)** - Note operations procedures

### Trigger Designs
- **[triggers/](triggers/)** - Trigger patterns and designs

---

## ⚠️ Important Notes

1. **Files may use old naming conventions** (e.g., `id` instead of `user_id`)
   - These are reference designs, not deployment-ready scripts
   - See `DATABASE-CURRENT/` for actual column names

2. **Some features are not implemented**
   - Audit logs
   - Materialized views
   - Full-text search
   - Advanced caching

3. **Design may evolve**
   - This is a living document
   - Future refactoring may change structure
   - Always check `DATABASE-CURRENT/` for truth

---

## 🚀 Future Phases

### Phase 2 (Planned)
- Advanced tag features (templates, bulk operations)
- Tag sharing and collaboration
- Materialized views for performance
- Full-text search on notes

### Phase 3 (Future)
- Audit logging
- Additional entity types (projects, documents, tasks)
- Advanced caching strategies
- Graph relationship queries

---

**Last Updated:** October 16, 2025
**Status:** Design reference only
**For deployment:** Use `DATABASE-CURRENT/` folder
