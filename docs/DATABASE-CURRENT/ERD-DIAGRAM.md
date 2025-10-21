# Entity Relationship Diagram (Detailed)

**Database:** SuperApp-dev (MVP 1.0)
**Last Updated:** October 15, 2025

---

## 📊 Full ERD with Relationships

```mermaid
erDiagram
    %% Core Tables
    users ||--o{ tags : "owns"
    users ||--o{ workspaces : "creates"
    users ||--o{ notes : "creates"

    %% Workspace relationships
    workspaces ||--o{ workspace_members : "has"
    workspaces ||--o{ workspace_relationship_types : "defines"
    workspaces ||--o{ workspace_items : "contains"

    users ||--o{ workspace_members : "member of"

    %% Tags relationships
    tags ||--o{ workspace_items : "parent"
    tags ||--o{ workspace_items : "child (if type=tag)"

    %% Entity types
    entity_types ||--o{ workspace_items : "defines type"

    %% Workspace items (UNIFIED)
    workspace_items }o--|| entity_types : "child_type"

    %% Notes relationships
    notes ||--o{ note_members : "shared with"
    notes ||--o{ note_versions : "has versions"
    notes ||--o{ workspace_items : "child (if type=note)"

    users ||--o{ note_members : "collaborator"
    users ||--o{ note_versions : "created by"

    %% Table Definitions
    users {
        int user_id PK
        nvarchar email UK
        nvarchar username UK
        nvarchar password_hash
        nvarchar display_name
        nvarchar avatar_url
        nvarchar bio
        nvarchar preferences "JSON"
        bit is_active
        bit email_verified
        datetime2 created_at
        datetime2 updated_at
        datetime2 last_login_at
        datetime2 deleted_at
    }

    tags {
        int tag_id PK
        int user_id FK
        nvarchar name
        nvarchar slug "auto-generated"
        nvarchar color "hex"
        nvarchar icon
        nvarchar description
        nvarchar metadata "JSON"
        int usage_count "auto-updated"
        datetime2 created_at
        datetime2 updated_at
        datetime2 deleted_at
    }

    entity_types {
        nvarchar type_name PK
        nvarchar display_name
        nvarchar display_plural
        nvarchar icon
        nvarchar color
        nvarchar description
        nvarchar table_name
        bit supports_versioning
        bit supports_sharing
        bit is_enabled
        datetime2 created_at
        datetime2 updated_at
    }

    workspaces {
        int workspace_id PK
        int user_id FK
        nvarchar name
        nvarchar description
        nvarchar color "hex"
        nvarchar icon
        nvarchar type "hierarchy/graph/network/timeline/custom"
        int max_depth
        bit is_default
        bit is_public
        bit is_template
        bit is_archived
        int tag_count "auto-updated"
        int relationship_count "auto-updated"
        int member_count "auto-updated"
        nvarchar settings "JSON"
        datetime2 created_at
        datetime2 updated_at
        datetime2 last_accessed_at
        datetime2 deleted_at
    }

    workspace_members {
        bigint member_id PK
        int workspace_id FK
        int user_id FK
        nvarchar role "owner/editor/viewer"
        int invited_by FK
        nvarchar invitation_status "pending/active/declined/removed"
        nvarchar custom_permissions "JSON"
        datetime2 invited_at
        datetime2 joined_at
        datetime2 last_accessed_at
        datetime2 deleted_at
    }

    workspace_relationship_types {
        int relationship_type_id PK
        int workspace_id FK
        nvarchar type_name
        nvarchar display_name
        nvarchar description
        nvarchar icon
        nvarchar color
        nvarchar line_style "solid/dashed/dotted"
        int line_width
        bit is_bidirectional
        bit allows_cycles
        int max_depth
        nvarchar validation_rules "JSON"
        int sort_order
        datetime2 created_at
        datetime2 updated_at
        datetime2 deleted_at
    }

    workspace_items {
        bigint item_id PK "UNIFIED TABLE"
        int workspace_id FK
        int parent_tag_id FK "NULL for root items"
        nvarchar child_type FK "tag/note/etc"
        int child_id "tag_id or note_id"
        nvarchar relationship_type
        nvarchar label
        nvarchar notes "relationship notes"
        nvarchar item_path "materialized path"
        int depth
        int sort_order
        nvarchar color
        nvarchar icon
        int added_by FK
        datetime2 created_at
        datetime2 updated_at
        datetime2 deleted_at
    }

    notes {
        int note_id PK
        int user_id FK
        nvarchar name
        nvarchar description
        nvarchar content "markdown"
        nvarchar slug "auto-generated"
        nvarchar color "hex"
        nvarchar icon
        bit is_archived
        bit is_pinned
        bit is_favorite
        int word_count "auto-calculated"
        int version_count "auto-updated"
        datetime2 created_at
        datetime2 updated_at
        datetime2 deleted_at
    }

    note_members {
        bigint member_id PK
        int note_id FK
        int user_id FK
        nvarchar role "owner/editor/viewer"
        int invited_by FK
        nvarchar invitation_status "pending/active/declined/removed"
        datetime2 invited_at
        datetime2 joined_at
        datetime2 last_accessed_at
        datetime2 deleted_at
    }

    note_versions {
        bigint version_id PK
        int note_id FK
        int version_number
        nvarchar name "snapshot"
        nvarchar description "snapshot"
        nvarchar content "full snapshot"
        int word_count "snapshot"
        nvarchar change_summary
        int created_by FK
        datetime2 created_at
    }
```

---

## 🔑 Key Relationships

### Core Flow
1. **User** creates **Workspaces** and **Tags**
2. **Workspace** has **Members** (sharing)
3. **Workspace** contains **Items** (tag-tag or tag-note relationships)
4. **User** creates **Notes** with **Members** and **Versions**

### UNIFIED workspace_items
- Parent can be **NULL for root items** or **tag** (`parent_tag_id`)
- Child can be **any entity** (`child_type` + `child_id`)
- Examples:
  - Root Tag "Work": `(parent_tag_id=NULL, child_type='tag', child_id=1, depth=0)`
  - Tag "Work" → Tag "Projects": `(parent_tag_id=1, child_type='tag', child_id=2, depth=1)`
  - Tag "Projects" → Note "Q1": `(parent_tag_id=2, child_type='note', child_id=5, depth=2)`

---

## 📝 Table Counts (Current MVP)

| Table | Rows | Status |
|-------|------|--------|
| users | 5 | ✅ Test data |
| tags | 9 | ✅ Test data |
| entity_types | 3 | ✅ Seeded (tag, note, + 3 disabled) |
| workspaces | 5 | ✅ Test data |
| workspace_members | 17 | ✅ Test data |
| workspace_relationship_types | 5 | ✅ Auto-created by trigger |
| workspace_items | 0 | ✅ Ready (no data yet) |
| notes | 12 | ✅ Test data |
| note_members | 12 | ✅ Auto-created by trigger |
| note_versions | 12 | ✅ Auto-created by trigger |

---

## 🎨 How to View

### Option 1: GitHub
- Upload this file to GitHub
- GitHub will auto-render Mermaid diagrams

### Option 2: VS Code
- Install "Markdown Preview Mermaid Support" extension
- Open this file and click "Open Preview"

### Option 3: Online
- Copy diagram to https://mermaid.live/
- Export as PNG/SVG

### Option 4: CLI
```bash
# Install mermaid-cli
npm install -g @mermaid-js/mermaid-cli

# Generate PNG
mmdc -i ERD-DIAGRAM.md -o ERD-DIAGRAM.png

# Generate SVG
mmdc -i ERD-DIAGRAM.md -o ERD-DIAGRAM.svg
```

---

**Note:** This diagram reflects the **ACTUAL deployed schema** (MVP 1.0), not the planned design.
