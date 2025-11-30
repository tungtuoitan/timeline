# Database Tables Summary

**Last Updated**: 2025-01-30
**Database**: SuperApp-dev
**Status**: ✅ Fresh schema (all tables dropped and recreated)

---

## 🔄 **IMPORTANT UPDATE (2025-01-30)**

**User đã xóa hết bảng cũ!**
→ Không cần migration nữa
→ Sử dụng `CREATE_DATABASE_SCHEMA.sql` để tạo lại từ đầu

---

## ✅ Active Tables (Currently in Use)

### 1. Core Tables

| Table Name | EF Entity | Description | Status |
|------------|-----------|-------------|--------|
| `users` | `User` | User accounts and authentication | ✅ Active |
| `user_profiles` | `UserProfile` | User profile information | ✅ Active |
| `tags_new` | `Tag` | **Hashtags** for polymorphic tagging | ✅ Active |
| `folders` | `Folder` | **Folders** (renamed from old tags table) | ✅ Active |
| `entity_types` | `EntityType` | Lookup table for entity types | ✅ Active |
| `standard_registries` | `StandardRegistry` | System configuration | ✅ Active |

### 2. Workspace Tables

| Table Name | EF Entity | Description | Status |
|------------|-----------|-------------|--------|
| `workspaces` | `Workspace` | Workspaces/projects | ✅ Active |
| `workspace_members` | `WorkspaceMember` | Workspace membership | ✅ Active |
| `workspace_relationship_types` | `WorkspaceRelationshipType` | Relationship types between workspaces | ✅ Active |
| `workspace_items` | `WorkspaceItem` | **Polymorphic items** (folders/notes/files) in workspace | ✅ Active |

### 3. Entity Tables

| Table Name | EF Entity | Description | Status |
|------------|-----------|-------------|--------|
| `notes` | `Note` | Notes/documents | ✅ Active |
| `note_members` | `NoteMember` | Note sharing and permissions | ✅ Active |
| `note_versions` | `NoteVersion` | Note version history | ✅ Active |
| `files` | `FileInfo` | File attachments | ✅ Active |

### 4. Tagging System (New)

| Table Name | EF Entity | Description | Status |
|------------|-----------|-------------|--------|
| `entity_tags` | `EntityTag` | **Polymorphic tagging** for all entities (workspace/folder/note/file) | ✅ Active |

---

## ❌ Deprecated Tables (To Be Removed)

| Table Name | Replacement | Migration Status | Action Required |
|------------|-------------|------------------|-----------------|
| `note_tags` | `entity_tags` | ✅ Data migrated | 🗑️ Run `CLEANUP_DROP_NOTE_TAGS.sql` to drop |

---

## 📊 Table Relationships

### Tags vs Folders vs EntityTags

```
Old Schema (Before Migration):
┌─────────┐
│  tags   │ ← (Used for both folders AND hashtags)
└─────────┘
     ↓
┌──────────┐
│note_tags │ ← (Many-to-many: notes ↔ tags)
└──────────┘

New Schema (After Migration):
┌─────────┐               ┌──────────┐
│ folders │               │tags_new  │ ← Hashtags only
│(renamed │               │(hashtags)│
│from tags)│               └─────┬────┘
└────┬────┘                     │
     │                          │
     ↓                          ↓
┌────────────────┐      ┌──────────────┐
│workspace_items │      │ entity_tags  │ ← Polymorphic tagging
│(polymorphic)   │      │ (polymorphic)│
└────────────────┘      └──────────────┘
```

### Key Points:

1. **`tags_new`**: Contains only hashtags (lightweight labels)
2. **`folders`**: Contains folders (hierarchical containers in workspaces)
3. **`entity_tags`**: Junction table linking hashtags to ANY entity (workspace/folder/note/file)
4. **`workspace_items`**: Junction table for items IN a workspace (folders/notes/files)

---

## 🔧 Recent Changes (2025-01-30)

### ✅ Completed:

1. ✅ Created `FolderConfiguration.cs` - Maps Folder entity to `folders` table
2. ✅ Created `EntityTagConfiguration.cs` - Maps EntityTag entity to `entity_tags` table
3. ✅ Updated `ApplicationDbContext.cs`:
   - Added `DbSet<Folder> Folders`
   - Added `DbSet<EntityTag> EntityTags`
   - Commented out `DbSet<NoteTag> NoteTags` (deprecated)
4. ✅ Renamed `NoteTagConfiguration.cs` → `NoteTagConfiguration.cs.deprecated`
5. ✅ Fixed `TagConfiguration.cs` to map to `tags_new` table (was incorrectly mapped to `tags`)
6. ✅ Created cleanup script `CLEANUP_DROP_NOTE_TAGS.sql`

### 🔄 Next Steps:

1. **Restart backend** to apply EF Core configuration changes
2. **Run cleanup script** to drop `note_tags` table:
   ```sql
   -- Run this in SQL Server Management Studio or sqlcmd
   USE SuperApp-dev;
   GO
   :r C:\Users\Admin\source\super-app\SuperApp-backend\migrations\CLEANUP_DROP_NOTE_TAGS.sql
   ```
3. **Test APIs**:
   - `/api/tags` - Should return hashtags from `tags_new`
   - `/api/notes` - Should load tags from `entity_tags` instead of `note_tags`
   - `/api/workspace/{id}/tree` - Should load folders from `folders` table

---

## 📝 EF Core Entity Configuration Files

| Configuration File | Table Mapping | Status |
|-------------------|---------------|--------|
| `UserConfiguration.cs` | `users` | ✅ Active |
| `UserProfileConfiguration.cs` | `user_profiles` | ✅ Active |
| `TagConfiguration.cs` | `tags_new` | ✅ Active (Fixed) |
| `FolderConfiguration.cs` | `folders` | ✅ Active (New) |
| `EntityTagConfiguration.cs` | `entity_tags` | ✅ Active (New) |
| `WorkspaceConfiguration.cs` | `workspaces` | ✅ Active |
| `WorkspaceItemConfiguration.cs` | `workspace_items` | ✅ Active |
| `NoteConfiguration.cs` | `notes` | ✅ Active |
| `FileConfiguration.cs` | `files` | ✅ Active |
| `NoteTagConfiguration.cs` | `note_tags` | ⚠️ Deprecated |

---

## 🚀 Migration Files

| Migration File | Description | Status |
|---------------|-------------|--------|
| `MIGRATION_TAGS_TO_FOLDERS_WITH_SHARE_FORK.sql` | Main migration: tags → folders, create tags_new | ✅ Applied |
| `MIGRATION_PATCH_NOTE_TAGS_TO_ENTITY_TAGS.sql` | Patch: note_tags → entity_tags | ✅ Applied |
| `CLEANUP_DROP_NOTE_TAGS.sql` | Cleanup: drop note_tags table | 🔄 Ready to run |

---

## ⚠️ Important Notes

1. **Do NOT query `tags` table** - it has been renamed to `folders`
2. **Use `tags_new`** for hashtag queries
3. **Use `entity_tags`** for polymorphic tagging (replaces `note_tags`)
4. **Backend must be restarted** after EF Core configuration changes
5. **Run cleanup script** to remove deprecated `note_tags` table

---

**End of Database Summary**
