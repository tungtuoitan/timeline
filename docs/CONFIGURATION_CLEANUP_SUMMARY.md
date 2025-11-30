# Configuration Cleanup Summary

## ✅ Configurations DELETED (Tables không tồn tại)

1. ❌ **NoteMemberConfiguration.cs** - Bảng `note_members` đã bị dropped
2. ❌ **NoteVersionConfiguration.cs** - Bảng `note_versions` đã bị dropped  
3. ❌ **WorkspaceMemberConfiguration.cs** - Bảng `workspace_members` đã bị dropped
4. ❌ **WorkspaceRelationshipTypeConfiguration.cs** - Bảng `workspace_relationship_types` đã bị dropped
5. ❌ **EntityTypeConfiguration.cs** - Bảng `entity_types` đã bị dropped (dùng `dbo.entities` thay thế)
6. ❌ **EntityTagConfiguration.cs** - Bảng `entity_tags` đã bị dropped (dùng `dbo.entity_hashtags` thay thế)
7. ❌ **TagConfiguration.cs** - Bảng `tags_new` đã bị dropped (dùng `dbo.hashtags` thay thế)

## ⚠️ Configurations CẦN FIX (Column/Table names sai)

### 1. UserConfiguration.cs
- ❌ Map `user_id` → ✅ Phải là `id`  
- Schema: `urm.users`

### 2. FolderConfiguration.cs  
- ❌ Map `folder_id` → ✅ Phải là `id`
- ❌ Schema `dbo` → ✅ Phải là `ws`  
- ❌ Table `folders` → ✅ Phải là `ws.folders`

### 3. FileConfiguration.cs
- ❌ Map `file_id` → ✅ Phải là `id`
- ❌ Schema `dbo` → ✅ Phải là `ws`
- ❌ Table `files` → ✅ Phải là `ws.files`

### 4. NoteConfiguration.cs
- ❌ Map `note_id` → ✅ Phải là `id`  
- Schema: `dbo.notes` ✅ ĐÚNG
- ✅ Added: Ignore `Members` và `Versions` navigation properties

### 5. StandardRegistryConfiguration.cs
- ❌ Table `standard_registry` → ✅ Phải là `standard_registries` (số nhiều)
- Schema: `dbo` ✅ ĐÚNG

### 6. WorkspaceConfiguration.cs
- ✅ FIXED: Changed `workspace_id` → `id`
- ✅ FIXED: Ignored 11 properties không tồn tại (Color, Icon, Type, MaxDepth, IsDefault, IsPublic, IsTemplate, IsArchived, TagCount, MemberCount, Settings, LastAccessedAt)
- ✅ FIXED: Ignored `Members` và `RelationshipTypes` navigation properties
- Schema: `ws.workspaces` ✅ ĐÚNG

### 7. WorkspaceItemConfiguration.cs
- ✅ ĐÚNG: Map `id` (không phải `item_id`)
- Schema: `ws.workspace_items` ✅ ĐÚNG

### 8. UserProfileConfiguration.cs
- ⚠️ CẦN KIỂM TRA: Schema `urm` có đúng không? PK là `Email` hay `id`?

## 📋 Schema Mới (11 bảng)

| # | Schema | Table | PK Column | Status |
|---|--------|-------|-----------|--------|
| 1 | urm | users | `id` | ⚠️ Fix UserId → id |
| 2 | urm | user_profiles | `id` (FK: user_id) | ⚠️ Kiểm tra |
| 3 | dbo | hashtags | `id` | ❌ Chưa có config |
| 4 | dbo | entities | `id` (TINYINT) | ❌ Chưa có config |
| 5 | dbo | standard_registries | `id` | ⚠️ Fix tên table |
| 6 | ws | workspaces | `id` | ✅ Fixed |
| 7 | ws | folders | `id` | ⚠️ Fix schema + PK |
| 8 | dbo | notes | `id` | ⚠️ Fix PK |
| 9 | ws | files | `id` | ⚠️ Fix schema + PK |
| 10 | ws | workspace_items | `id` | ✅ Đúng |
| 11 | dbo | entity_hashtags | `id` | ❌ Chưa có config |

## 🔧 Cần tạo mới

1. **HashtagConfiguration.cs** - Map `dbo.hashtags`
2. **EntityConfiguration.cs** - Map `dbo.entities` (lookup table)
3. **EntityHashtagConfiguration.cs** - Map `dbo.entity_hashtags` (polymorphic tagging)

## 📝 DbContext Updates

✅ **FIXED**: Xóa các DbSet không tồn tại:
- ❌ DbSet<Tag> Tags
- ❌ DbSet<EntityType> EntityTypes
- ❌ DbSet<WorkspaceMember> WorkspaceMembers
- ❌ DbSet<WorkspaceRelationshipType> WorkspaceRelationshipTypes
- ❌ DbSet<NoteMember> NoteMembers
- ❌ DbSet<NoteVersion> NoteVersions
- ❌ DbSet<EntityTag> EntityTags

⏳ **CẦN THÊM** (sau khi tạo models mới):
- DbSet<Hashtag> Hashtags
- DbSet<Entity> Entities  
- DbSet<EntityHashtag> EntityHashtags

## 🎯 Action Items

1. ✅ Delete 7 configuration files không dùng
2. ⏳ Fix 5 configurations (User, Folder, File, Note, StandardRegistry)
3. ⏳ Kiểm tra UserProfileConfiguration
4. ⏳ Tạo 3 models mới (Hashtag, Entity, EntityHashtag)
5. ⏳ Tạo 3 configurations mới
6. ⏳ Update DbContext với DbSets mới
7. ✅ Fix WorkspaceConfiguration (DONE)
8. ✅ Fix NoteConfiguration (DONE - ignored Members/Versions)
9. ✅ Update ApplicationDbContext (DONE - removed old DbSets)
