# Schema vs Code Mismatch Analysis & Fix Summary

**Date:** 2025-11-30  
**Issue:** Code configurations and models don't match REBUILD_SIMPLIFIED_SCHEMA.sql

---

## ✅ COMPLETED: EF Core Configurations Updated

All configurations have been updated to **EXACTLY match** the REBUILD_SIMPLIFIED_SCHEMA.sql:

### 1. **UserConfiguration** → `urm.users`
```sql
-- Schema columns:
id, email, phone, password, auth_type, is_active, last_login_at, created_at, updated_at, deleted_at
```
✅ Updated configuration to map these exact columns

### 2. **UserProfileConfiguration** → `urm.user_profiles`
```sql
-- Schema columns:
id, user_id, first_name, last_name, avatar_url, bio, date_of_birth, gender, country, city, timezone, language, created_at, updated_at
```
✅ Updated configuration to map these exact columns

### 3. **NoteConfiguration** → `dbo.notes`
```sql
-- Schema columns (SIMPLIFIED):
id, user_id, name, description, created_at, updated_at, deleted_at
```
✅ Removed all extra fields (content, slug, color, icon, word_count, version_count, is_archived, is_pinned, is_favorite)

### 4. **FolderConfiguration** → `ws.folders`
```sql
-- Schema columns:
id, user_id, name, description, color, icon, created_at, updated_at, deleted_at
```
✅ Removed extra fields (slug, metadata, usage_count)

### 5. **FileConfiguration** → `ws.files`
```sql
-- Schema columns:
id, user_id, name, url, file_size, mime_type, extension, created_at, updated_at, deleted_at
```
✅ Removed extra fields (original_filename, file_path, description, slug, is_public, is_archived, is_pinned, is_favorite, download_count, last_downloaded_at)

### 6. **WorkspaceConfiguration** → `ws.workspaces`
```sql
-- Schema columns:
id, user_id, name, description, created_at, updated_at, deleted_at
```
✅ Already had correct Ignore statements for non-existent fields

### 7. **WorkspaceItemConfiguration** → `ws.workspace_items`
```sql
-- Schema columns:
id, workspace_id, folder_id, item_type, item_id, is_original, created_at, updated_at, deleted_at
```
✅ Already correct

### 8. **StandardRegistryConfiguration** → `dbo.standard_registries`
```sql
-- Schema columns:
id, type_code, description, is_active, created_at, updated_at
```
✅ Updated from (code, type, active, created_by)

### 9. **NEW: HashtagConfiguration** → `dbo.hashtags`
```sql
-- Schema columns:
id, user_id, name, usage_count, created_at, updated_at, deleted_at
```
✅ Created - maps Tag model to hashtags table

### 10. **NEW: EntityHashtagConfiguration** → `dbo.entity_hashtags`
```sql
-- Schema columns:
id, entity_type, entity_id, hashtag_id, created_at
```
✅ Created - maps EntityTag model to entity_hashtags table

### 11. **NEW: EntityLookupConfiguration** → `dbo.entities`
```sql
-- Schema columns:
id (TINYINT), name, description, created_at
-- Seed data: 1=workspace, 2=folder, 3=note, 4=file
```
✅ Created with seed data

---

## ❌ REMAINING ISSUE: Model Classes Don't Match Schema

**Build errors:** 22 errors because Model classes have wrong properties.

### Models That Need Updating:

#### 1. **User.cs** - urm.users
**Current properties (WRONG):**
- UserId, Email, Username, PasswordHash, DisplayName, AvatarUrl, Bio, Preferences, EmailVerified, IsActive, CreatedAt, UpdatedAt, LastLoginAt, DeletedAt

**Should be (CORRECT per schema):**
```csharp
public class User : ITimestampEntity
{
    public int UserId { get; set; }           // id
    public string Email { get; set; }          // email (required, unique)
    public string? Phone { get; set; }         // phone
    public string Password { get; set; }       // password (required)
    public string AuthType { get; set; } = "local"; // auth_type ('local', 'google', 'facebook')
    public bool IsActive { get; set; } = true; // is_active
    public DateTime? LastLoginAt { get; set; } // last_login_at
    public DateTime? CreatedAt { get; set; }   // created_at
    public DateTime? UpdatedAt { get; set; }   // updated_at
    public DateTime? DeletedAt { get; set; }   // deleted_at
    
    // Navigation
    public ICollection<Workspace> Workspaces { get; set; }
    public ICollection<Note> Notes { get; set; }
    // No more: Tags (renamed to Hashtags via Tag model)
}
```

**REMOVE:** Username, PasswordHash, DisplayName, AvatarUrl, Bio, Preferences, EmailVerified  
**ADD:** Phone, Password (instead of PasswordHash), AuthType

---

#### 2. **UserProfile.cs** - urm.user_profiles
**Current properties (COMPLETELY WRONG):**
- Email (PK), AppC, Parents, Priorities, Statuses, Types, RepeatTypes, IsUpdatedTodays, CreatedAt, UpdatedAt, IsActive

**Should be (CORRECT per schema):**
```csharp
public class UserProfile
{
    public int Id { get; set; }                 // id (PK)
    public int UserId { get; set; }             // user_id (FK, unique)
    public string? FirstName { get; set; }      // first_name
    public string? LastName { get; set; }       // last_name
    public string? AvatarUrl { get; set; }      // avatar_url
    public string? Bio { get; set; }            // bio
    public DateTime? DateOfBirth { get; set; }  // date_of_birth
    public string? Gender { get; set; }         // gender
    public string? Country { get; set; }        // country
    public string? City { get; set; }           // city
    public string? Timezone { get; set; } = "UTC"; // timezone
    public string? Language { get; set; } = "en";  // language
    public DateTime? CreatedAt { get; set; }    // created_at
    public DateTime? UpdatedAt { get; set; }    // updated_at
    
    // Navigation
    public User User { get; set; }
}
```

**COMPLETE REDESIGN NEEDED** - current fields are completely different!

---

#### 3. **Note.cs** - dbo.notes
**Current properties (TOO MANY):**
- NoteId, UserId, Name, Description, Content, Slug, Color, Icon, WordCount, VersionCount, IsArchived, IsPinned, IsFavorite, CreatedAt, UpdatedAt, DeletedAt
- Navigation: User, Members, Versions

**Should be (SIMPLIFIED per schema):**
```csharp
public class Note : ITimestampEntity
{
    public int NoteId { get; set; }        // id
    public int UserId { get; set; }        // user_id
    public string Name { get; set; }       // name (required)
    public string? Description { get; set; } // description (nvarchar(max))
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Navigation
    public User User { get; set; }
    // REMOVE: Members, Versions (tables don't exist in simplified schema)
}
```

**REMOVE:** Content, Slug, Color, Icon, WordCount, VersionCount, IsArchived, IsPinned, IsFavorite  
**REMOVE Navigation:** Members, Versions

---

#### 4. **Folder.cs** - ws.folders
**Current properties:**
- FolderId, UserId, Name, Slug, Color, Icon, Description, Metadata, UsageCount, CreatedAt, UpdatedAt, DeletedAt
- Navigation: User, WorkspaceItems, EntityTags

**Should be:**
```csharp
public class Folder : ITimestampEntity
{
    public int FolderId { get; set; }      // id
    public int UserId { get; set; }        // user_id
    public string Name { get; set; }       // name (required)
    public string? Description { get; set; } // description (nvarchar(max))
    public string? Color { get; set; } = "#F59E0B"; // color
    public string? Icon { get; set; } = "📁"; // icon
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Navigation
    public User User { get; set; }
    public ICollection<WorkspaceItem> WorkspaceItems { get; set; }
}
```

**REMOVE:** Slug, Metadata, UsageCount  
**REMOVE Navigation:** EntityTags

---

#### 5. **FileInfo.cs** - ws.files
**Current properties:**
- FileId, UserId, Name, OriginalFilename, FilePath, FileSize, MimeType, Extension, Description, Slug, IsPublic, IsArchived, IsPinned, IsFavorite, DownloadCount, LastDownloadedAt, CreatedAt, UpdatedAt, DeletedAt

**Should be:**
```csharp
public class FileInfo : ITimestampEntity
{
    public int FileId { get; set; }        // id
    public int UserId { get; set; }        // user_id
    public string Name { get; set; }       // name (required)
    public string? Url { get; set; }       // url
    public long? FileSize { get; set; }    // file_size
    public string? MimeType { get; set; }  // mime_type
    public string? Extension { get; set; } // extension
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Navigation
    public User User { get; set; }
}
```

**REMOVE:** OriginalFilename, FilePath, Description, Slug, IsPublic, IsArchived, IsPinned, IsFavorite, DownloadCount, LastDownloadedAt  
**ADD:** Url

---

#### 6. **StandardRegistry.cs** - dbo.standard_registries
**Current properties:**
- Id, Code, Description, Type, Active (int), CreatedAt, UpdatedAt, CreatedBy

**Should be:**
```csharp
public class StandardRegistry
{
    public int Id { get; set; }             // id
    public string TypeCode { get; set; }    // type_code (unique, required)
    public string? Description { get; set; } // description
    public bool IsActive { get; set; } = true; // is_active (BIT)
    public DateTime? CreatedAt { get; set; } // created_at
    public DateTime? UpdatedAt { get; set; } // updated_at
}
```

**RENAME:** Code → TypeCode, Type → (removed), Active (int) → IsActive (bool)  
**REMOVE:** CreatedBy

---

## 📋 Action Plan

### Option 1: Update Models to Match Schema ✅ RECOMMENDED
**Pros:** Clean, matches database exactly, no technical debt  
**Cons:** Requires updating all code that uses these models

**Steps:**
1. Update User model (add Phone, Password, AuthType; remove Username, PasswordHash, DisplayName, etc.)
2. **COMPLETELY REDESIGN** UserProfile model
3. Simplify Note model (remove Content, Slug, Color, Icon, flags, counts)
4. Simplify Folder model (remove Slug, Metadata, UsageCount)
5. Simplify FileInfo model (remove many fields, add Url)
6. Update StandardRegistry model (rename fields, change Active type)
7. Update all repositories using these models
8. Update all services using these models
9. Update all DTOs
10. Update all controllers

### Option 2: Change Database Schema ❌ NOT RECOMMENDED
**Pros:** Keep existing code  
**Cons:** Contradicts your REBUILD_SIMPLIFIED_SCHEMA goal, adds complexity back

---

## 🔍 Build Errors Summary

**Total:** 22 errors
- **User model:** 5 errors (Phone, Password, AuthType missing)
- **UserProfile model:** 14 errors (completely wrong structure)
- **FileInfo model:** 1 error (Url missing)
- **StandardRegistry model:** 2 errors (TypeCode missing, IsActive missing)

---

## ✅ Recommendation

Since you created REBUILD_SIMPLIFIED_SCHEMA.sql to **simplify** the database, you should:

1. **Update all Model classes** to match the simplified schema
2. **Remove deprecated/extra fields** that don't exist in database
3. **Update Repositories** to work with simplified models
4. **Update DTOs** to match simplified models
5. **Test thoroughly** after changes

This will give you a clean, maintainable codebase that matches your simplified database design.

---

## 📝 Next Steps

Would you like me to:
1. ✅ Update all Model classes to match the schema?
2. ✅ Update Repositories after models are fixed?
3. ✅ Update DTOs after models are fixed?
4. ✅ Update Controllers after models are fixed?

Or would you prefer to review the changes first and decide which approach to take?
