# Refactoring Summary: FolderId → ParentId

## Ngày: 2025-12-01

## Mục đích
Đổi tên field `FolderId` → `ParentId` trong toàn bộ codebase và database để rõ ràng hơn về ý nghĩa: field này đại diện cho **Parent Folder ID** (folder chứa item hiện tại), không phải ID của chính folder đó.

## Thay đổi trong Code

### 1. **Models**
- `WorkspaceItem.FolderId` → `WorkspaceItem.ParentId`
- `WorkspaceItem` constructor: `folderId` parameter → `parentId`

### 2. **DTOs**
- `UpsertFolderRequest.FolderId` (parent) → `ParentId`
- `MoveItemsRequest.TargetFolderId` → `TargetParentId`
- `ShareFolderRequest.ParentFolderId` → `ParentId`

### 3. **Repositories**
- `IWorkspaceRepository.UpsertFolderAsync()`: `parentFolderId` → `parentId`
- `IWorkspaceRepository.MoveItemsAsync()`: `targetFolderId` → `targetParentId`
- `WorkspaceRepository`: Tất cả tham số và biến liên quan

### 4. **Services**
- `WorkspaceService.UpsertFolderAsync()`: Sử dụng `request.ParentId`
- `WorkspaceService.MoveItemsAsync()`: Sử dụng `request.TargetParentId`

### 5. **Configurations**
- `WorkspaceItemConfiguration`: 
  - Column name: `folder_id` → `parent_id`
  - Index name: `idx_workspace_items_folder_id` → `idx_workspace_items_parent_id`
  - FK property: `FolderId` → `ParentId`

## Thay đổi trong Database

### Migration Script
File: `migrations/RENAME_FOLDER_ID_TO_PARENT_ID.sql`

**Steps:**
1. Drop FK constraint: `fk_workspace_items_parent_folder`
2. Drop index: `idx_workspace_items_folder_id`
3. Rename column: `folder_id` → `parent_id`
4. Recreate FK constraint với `parent_id`
5. Recreate index: `idx_workspace_items_parent_id`

### Stored Procedure
File: `migrations/sp_MoveWorkspaceItems.sql`

**Thay đổi:**
- Parameter: `@TargetFolderId` → `@TargetParentId`
- Column references: `folder_id` → `parent_id`
- Variable: `OldFolderId` → `OldParentId`

## Cách chạy Migration

### Bước 1: Backup Database
```sql
BACKUP DATABASE [SuperApp-dev] TO DISK = 'C:\Backups\SuperApp-dev_before_rename.bak';
```

### Bước 2: Chạy Migration Script
```sql
-- Chạy file migrations/RENAME_FOLDER_ID_TO_PARENT_ID.sql
-- hoặc sử dụng SSMS để execute
```

### Bước 3: Update Stored Procedure
```sql
-- Chạy file migrations/sp_MoveWorkspaceItems.sql
-- để update stored procedure với tên parameter mới
```

### Bước 4: Tạo EF Core Migration (nếu cần)
```bash
# Nếu dùng EF Core migrations
dotnet ef migrations add RenameParentIdColumn --project SuperAppDataRepositories --startup-project SuperAppAPI

# Review migration code, sau đó update database
dotnet ef database update --project SuperAppDataRepositories --startup-project SuperAppAPI
```

### Bước 5: Verify
```sql
-- Kiểm tra column đã đổi tên
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'ws' 
  AND TABLE_NAME = 'workspace_items'
  AND COLUMN_NAME = 'parent_id';

-- Kiểm tra index
SELECT name FROM sys.indexes 
WHERE name = 'idx_workspace_items_parent_id';

-- Kiểm tra FK constraint
SELECT name FROM sys.foreign_keys 
WHERE name = 'fk_workspace_items_parent_folder';
```

## Breaking Changes

### API Changes
Client cần update payload:

**TRƯỚC:**
```json
{
  "name": "My Folder",
  "folderId": 10  // ← Parent folder ID
}
```

**SAU:**
```json
{
  "name": "My Folder",
  "parentId": 10  // ← Rõ ràng hơn
}
```

**Move Items - TRƯỚC:**
```json
{
  "items": [...],
  "targetFolderId": 5
}
```

**Move Items - SAU:**
```json
{
  "items": [...],
  "targetParentId": 5
}
```

## Rollback Plan

Nếu có lỗi, rollback bằng cách:

```sql
-- 1. Restore backup
RESTORE DATABASE [SuperApp-dev] FROM DISK = 'C:\Backups\SuperApp-dev_before_rename.bak' WITH REPLACE;

-- 2. Hoặc đổi ngược lại
EXEC sp_rename 'ws.workspace_items.parent_id', 'folder_id', 'COLUMN';
-- Rồi recreate FK và index
```

## Checklist

- [x] Update Models (WorkspaceItem)
- [x] Update DTOs (UpsertFolderRequest, MoveItemsRequest, ShareFolderRequest)
- [x] Update Repositories (IWorkspaceRepository, WorkspaceRepository)
- [x] Update Services (WorkspaceService)
- [x] Update Configurations (WorkspaceItemConfiguration)
- [x] Create SQL migration script
- [x] Update stored procedure sp_MoveWorkspaceItems
- [ ] **Run migration SQL script**
- [ ] **Test API endpoints**
- [ ] **Update frontend/client code**
- [ ] Update API documentation/Swagger
- [ ] Update database schema documentation

## Testing

Sau khi migration, test các chức năng:

1. ✅ Tạo folder mới ở root (`parentId: null`)
2. ✅ Tạo folder con (`parentId: <id>`)
3. ✅ Update folder và di chuyển vị trí
4. ✅ Move items với `targetParentId`
5. ✅ Get workspace tree (kiểm tra hierarchy)

---

**Version:** 1.0
**Author:** SuperApp Team
**Date:** 2025-12-01
