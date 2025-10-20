# Sample Data for Testing

## Problem Analysis

The stored procedure `usp_s_tag_tree` returns no data because the `workspace_items` table is **empty**.

### How the Procedure Works

The procedure builds a tag tree hierarchy by:
1. Checking if the user has access to the workspace via `workspace_members`
2. Finding **root tags** - tags that are parents in `workspace_items` but NOT children
3. Recursively building the tree from those root tags
4. Including notes attached to tags

**Result:** Since `workspace_items` is empty, there are no parent-child relationships, so the procedure returns empty.

---

## Current Data

### Users (user_id = 1)
```
user_id: 1
email: admin@superapp.com
username: admin
display_name: System Administrator
```

### Workspaces (workspace_id = 1)
```
workspace_id: 1
user_id: 1
name: System Administration
description: Main workspace for system administration tasks
type: hierarchy
max_depth: 10
```

### Workspace Members
```
member_id: 63
workspace_id: 1
user_id: 1
role: owner
invitation_status: active
```

### Tags (for user_id = 1)
```
tag_id: 126
name: System
slug: system
color: #EF4444
icon: server
usage_count: 5

tag_id: 127
name: Important
slug: important
color: #DC2626
icon: star
usage_count: 10
```

### Notes (for user_id = 1)
```
note_id: 1
name: System Maintenance Checklist
slug: system-maintenance-checklist

note_id: 10
name: Security Incident Response Plan
slug: security-incident-response
```

### Workspace Items
```
EMPTY - This is why usp_s_tag_tree returns no data!
```

---

## Sample Data to Insert for Testing

### Option 1: Simple Tag Hierarchy

Create a simple hierarchy: System (tag 126) > Important (tag 127) > Note (note 1)

```sql
-- Make 'System' a root tag with 'Important' as child
INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, relationship_type, depth, sort_order, added_by, created_at)
VALUES (1, 126, 'tag', 127, 'contains', 1, 0, 1, GETDATE());

-- Add a note under 'Important' tag
INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, relationship_type, depth, sort_order, added_by, created_at)
VALUES (1, 127, 'note', 1, 'tagged_with', 2, 0, 1, GETDATE());
```

**Expected Result:**
```
System (level 0)
  └─ Important (level 1)
      └─ System Maintenance Checklist (level 2)
```

### Option 2: More Complex Hierarchy

First create more tags:

```sql
-- Insert additional tags
INSERT INTO tags (user_id, name, slug, color, icon, description, usage_count, created_at)
VALUES
(1, 'Projects', 'projects', '#3B82F6', 'folder', 'All projects', 8, GETDATE()),
(1, 'Active', 'active', '#10B981', 'play', 'Active items', 15, GETDATE()),
(1, 'Archive', 'archive', '#6B7280', 'archive', 'Archived items', 3, GETDATE());

-- Let's say this creates tag_ids: 150, 151, 152
```

Then create hierarchy:

```sql
-- System (126) as root with two children: Projects (150) and Important (127)
INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, relationship_type, depth, sort_order, added_by, created_at)
VALUES
(1, 126, 'tag', 150, 'contains', 1, 0, 1, GETDATE()),
(1, 126, 'tag', 127, 'contains', 1, 1, 1, GETDATE());

-- Projects (150) has two children: Active (151) and Archive (152)
INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, relationship_type, depth, sort_order, added_by, created_at)
VALUES
(1, 150, 'tag', 151, 'contains', 2, 0, 1, GETDATE()),
(1, 150, 'tag', 152, 'contains', 2, 1, 1, GETDATE());

-- Add notes to Active tag
INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, relationship_type, depth, sort_order, added_by, created_at)
VALUES
(1, 151, 'note', 1, 'tagged_with', 3, 0, 1, GETDATE()),
(1, 151, 'note', 10, 'tagged_with', 3, 1, 1, GETDATE());
```

**Expected Result:**
```
System (level 0)
  ├─ Projects (level 1)
  │   ├─ Active (level 2)
  │   │   ├─ System Maintenance Checklist (level 3)
  │   │   └─ Security Incident Response Plan (level 3)
  │   └─ Archive (level 2)
  └─ Important (level 1)
```

---

## Quick Test Commands

### Insert Simple Test Data
```sql
-- Simple hierarchy
INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, relationship_type, depth, sort_order, added_by, created_at)
VALUES
(1, 126, 'tag', 127, 'contains', 1, 0, 1, GETDATE()),
(1, 127, 'note', 1, 'tagged_with', 2, 0, 1, GETDATE());
```

### Test the Procedure
```sql
EXEC usp_s_tag_tree @workspace_id = 1, @user_id = 1;
```

### Verify Data
```sql
SELECT * FROM workspace_items WHERE workspace_id = 1 AND deleted_at IS NULL;
```

### Clean Up Test Data
```sql
DELETE FROM workspace_items WHERE workspace_id = 1;
```

---

## Database Connection

**Server:** TUNGHOMEPC\MSSQLSERVER03
**Database:** SuperApp-dev
**User:** sa
**Password:** Tung76721119@

```bash
sqlcmd -S "TUNGHOMEPC\MSSQLSERVER03" -U sa -P "Tung76721119@" -d SuperApp-dev
```
