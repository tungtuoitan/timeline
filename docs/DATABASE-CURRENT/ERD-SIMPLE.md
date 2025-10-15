# Entity Relationship Diagram (Simplified)

**Database:** SuperApp-dev (MVP 1.0)
**Purpose:** Quick overview without field details

---

## 📊 Simple ERD (Overview)

```mermaid
erDiagram
    %% Core entities
    users ||--o{ tags : owns
    users ||--o{ workspaces : creates
    users ||--o{ notes : creates

    %% Workspace system
    workspaces ||--o{ workspace_members : has
    workspaces ||--o{ workspace_items : contains
    users ||--o{ workspace_members : "member of"

    %% UNIFIED workspace_items
    tags ||--o{ workspace_items : "parent (always tag)"
    tags ||--o{ workspace_items : "child (if type=tag)"
    notes ||--o{ workspace_items : "child (if type=note)"

    %% Notes system
    notes ||--o{ note_members : "shared with"
    notes ||--o{ note_versions : versions
    users ||--o{ note_members : collaborator

    %% Table definitions (minimal)
    users {
        int user_id PK
        string email
        string username
    }

    tags {
        int tag_id PK
        int user_id FK
        string name
        string slug
    }

    workspaces {
        int workspace_id PK
        int user_id FK
        string name
        string type
    }

    workspace_members {
        bigint member_id PK
        int workspace_id FK
        int user_id FK
        string role
    }

    workspace_items {
        bigint item_id PK
        int workspace_id FK
        int parent_tag_id FK
        string child_type
        int child_id
    }

    notes {
        int note_id PK
        int user_id FK
        string name
        string content
    }

    note_members {
        bigint member_id PK
        int note_id FK
        int user_id FK
        string role
    }

    note_versions {
        bigint version_id PK
        int note_id FK
        int version_number
    }
```

---

## 🎯 Key Points

### Three Main Systems

1. **User & Tags**
   - Users own tags (global pool)
   - Tags used across workspaces

2. **Workspaces**
   - Users create workspaces
   - Workspaces shared with members (roles)
   - **workspace_items (UNIFIED)** organizes both tags and notes

3. **Notes**
   - Users create notes
   - Notes shared with members (roles)
   - Notes versioned automatically

---

## ⚠️ UNIFIED Design

**workspace_items** is the key table:
- **Parent:** Always a tag (`parent_tag_id`)
- **Child:** Can be tag OR note (`child_type` + `child_id`)

**Examples:**
```
Tag "Work" contains Tag "Projects"
├─ parent_tag_id: 1 (Work)
└─ child_type: 'tag', child_id: 2 (Projects)

Tag "Projects" contains Note "Q1 Report"
├─ parent_tag_id: 2 (Projects)
└─ child_type: 'note', child_id: 5 (Q1 Report)
```

---

## 📊 Statistics

- **10 tables** deployed
- **15 procedures** (CRUD operations)
- **8 triggers** (auto-generation, stats updates)
- **~35 indexes** (performance optimization)

---

**For detailed field-level ERD:** See [ERD-DIAGRAM.md](ERD-DIAGRAM.md)
