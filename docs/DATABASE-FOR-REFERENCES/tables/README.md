# Database Table Designs - FOR REFERENCE ONLY

**Purpose:** Design reference for table structures and patterns

---

## ⚠️ IMPORTANT WARNING

These SQL files are **DESIGN REFERENCES ONLY** and use old naming conventions.

**DO NOT use these files for deployment!**

---

## 📋 What's Here

### Core Tables
**Location:** `core/`

| File | Table | Purpose | Status |
|------|-------|---------|--------|
| `users.sql` | users | User accounts | 📋 Reference design |
| `tags.sql` | tags | Global tag pool | 📋 Reference design |
| `entity_types.sql` | entity_types | Entity registry | 📋 Reference design |

### Workspace Tables
**Location:** `workspace/`

| File | Table | Purpose | Status |
|------|-------|---------|--------|
| `workspaces.sql` | workspaces | Workspace containers | 📋 Reference design |
| `workspace_members.sql` | workspace_members | Sharing & permissions | 📋 Reference design |
| `workspace_relationship_types.sql` | workspace_relationship_types | Relationship definitions | 📋 Reference design |

### Entity Tables
**Location:** `entities/`

| File | Table | Purpose | Status |
|------|-------|---------|--------|
| `workspace_items.sql` | workspace_items | UNIFIED items table | 📋 Reference design |
| `notes.sql` | notes | Markdown notes | 📋 Reference design |
| `note_members.sql` | note_members | Note sharing | 📋 Reference design |
| `note_versions.sql` | note_versions | Version history | 📋 Reference design |

---

## ⚠️ Known Issues in These Files

### 1. Old Naming Convention
These files use **old column names**:
- ❌ Primary key: `id`
- ✅ Should be: `user_id`, `tag_id`, `workspace_id`, etc.

### 2. Incorrect Foreign Key References
- ❌ `REFERENCES users(id)`
- ✅ Should be: `REFERENCES users(user_id)`

### 3. Wrong Database Name
- ❌ `USE SuperApp;`
- ✅ Should be: `USE [SuperApp-dev];`

---

## ✅ For Actual Deployment

**Use the DATABASE-CURRENT folder instead:**

```
docs/DATABASE-CURRENT/
├── tables/core/users.sql        ✅ Correct naming
├── tables/core/tags.sql         ✅ Correct naming
├── tables/workspace/...         ✅ Correct naming
└── tables/entities/...          ✅ Correct naming
```

**Or see:**
- [`../../DATABASE-CURRENT/INDEX.md`](../../DATABASE-CURRENT/INDEX.md) - Current production schema
- [`../../DATABASE-CURRENT/ERD-DIAGRAM.md`](../../DATABASE-CURRENT/ERD-DIAGRAM.md) - Visual schema

---

## 💡 How to Use These Files

### ✅ DO Use For:
- Understanding table design patterns
- Learning business logic
- Planning future enhancements
- Reviewing trigger/constraint logic

### ❌ DON'T Use For:
- Deployment to database
- Writing application code
- Generating migration scripts
- Reference for column names

---

## 🔗 Related Documentation

| Document | Purpose | Location |
|----------|---------|----------|
| **Current Schema** | Production-ready table definitions | `../../DATABASE-CURRENT/tables/` |
| **Main Index** | Complete database design | `../INDEX.md` |
| **ERD Diagram** | Visual schema diagram | `../../DATABASE-CURRENT/ERD-DIAGRAM.md` |

---

**Last Updated:** October 16, 2025
**Status:** Design reference only
**For deployment:** Use `DATABASE-CURRENT/tables/` instead
