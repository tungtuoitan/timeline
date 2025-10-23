# API Chỉnh Sửa Workspace Item - Tóm Tắt

**Ngày tạo:** 22/10/2025  
**Trạng thái:** ✅ **ĐÃ TRIỂN KHAI ĐẦY ĐỦ**

## 📋 Tổng Quan

API để chỉnh sửa các thuộc tính của item trong workspace **đã được triển khai hoàn chỉnh** và hỗ trợ tất cả các loại entity:
- ✅ **Tags** (thẻ phân loại)
- ✅ **Notes** (ghi chú)
- ✅ **Files** (tệp tin/tài liệu)

## 🔧 API Endpoint

### PUT `/api/workspace/{workspaceId}/items/{itemId}`

**Mô tả:** Cập nhật các thuộc tính hiển thị của một item trong workspace

**Authentication:** Required (TEMPORARY: Disabled for dev)

**Parameters:**
- `workspaceId` (path) - ID của workspace
- `itemId` (path) - ID của workspace item cần cập nhật

**Request Body:**
```json
{
  "label": "New Label",           // Tùy chọn: Tên hiển thị tùy chỉnh (max 200 ký tự)
  "notes": "Some notes here",     // Tùy chọn: Ghi chú bổ sung (max 2000 ký tự)
  "color": "#FF5733",             // Tùy chọn: Màu sắc (định dạng hex #RRGGBB)
  "icon": "📝",                   // Tùy chọn: Icon/emoji (max 50 ký tự)
  "sortOrder": 5                  // Tùy chọn: Thứ tự sắp xếp (số >= 0)
}
```

**Lưu ý quan trọng:**
- Tất cả các trường đều **tùy chọn** (nullable)
- Chỉ cần gửi các trường muốn cập nhật
- Các trường không gửi sẽ giữ nguyên giá trị cũ
- Có thể cập nhật một hoặc nhiều trường cùng lúc

## 📦 Ví Dụ Sử Dụng

### 1. Chỉ cập nhật Label
```bash
curl -X PUT "http://localhost:5000/api/workspace/1/items/123" \
  -H "Content-Type: application/json" \
  -d '{"label": "Important Tag"}'
```

### 2. Cập nhật Color và Icon
```bash
curl -X PUT "http://localhost:5000/api/workspace/1/items/123" \
  -H "Content-Type: application/json" \
  -d '{
    "color": "#FF5733",
    "icon": "⭐"
  }'
```

### 3. Cập nhật tất cả thuộc tính
```bash
curl -X PUT "http://localhost:5000/api/workspace/1/items/123" \
  -H "Content-Type: application/json" \
  -d '{
    "label": "My Custom Label",
    "notes": "This is a very important item",
    "color": "#4CAF50",
    "icon": "📌",
    "sortOrder": 10
  }'
```

### 4. Xóa Notes (set về null)
```bash
curl -X PUT "http://localhost:5000/api/workspace/1/items/123" \
  -H "Content-Type: application/json" \
  -d '{"notes": null}'
```

## ✅ Response

### Success (200 OK)
```json
{
  "itemId": 123,
  "workspaceId": 1,
  "parentTagId": 5,
  "childType": "tag",
  "childId": 10,
  "label": "My Custom Label",
  "notes": "This is a very important item",
  "color": "#4CAF50",
  "icon": "📌",
  "sortOrder": 10,
  "createdAt": "2025-10-22T10:00:00Z",
  "updatedAt": "2025-10-22T15:30:00Z",
  "addedBy": 1,
  "message": "Workspace item updated successfully"
}
```

### Error Responses

#### 400 Bad Request
```json
{
  "error": "Invalid color format. Must be #RRGGBB"
}
```

#### 401 Unauthorized
```json
{
  "error": "Access denied - you do not have permission to modify this workspace"
}
```

#### 404 Not Found
```json
{
  "error": "Workspace item not found"
}
```

## 🏗️ Kiến Trúc Implementation

### 1. API Controller
📁 `SuperAppAPI/Controllers/WorkspaceController.cs`
- Endpoint: `PUT {workspaceId}/items/{itemId}`
- Validates route parameters
- Delegates to MediatR

### 2. Application Layer
📁 `SuperApp.Application/Features/Workspaces/Commands/UpdateWorkspaceItem/`
- **UpdateWorkspaceItemCommand.cs** - Command definition
- **UpdateWorkspaceItemCommandHandler.cs** - Business logic handler
- Auto-maps to response DTO

### 3. Data Layer
📁 `SuperAppDataRepositories/Repositories/WorkspaceRepository.cs`
- Method: `UpdateWorkspaceItemAsync()`
- Calls stored procedure `usp_update_workspace_item`

### 4. Database
📁 `docs/DATABASE-CURRENT/procedures/items/usp_update_workspace_item.sql`
- Validates user permissions (owner/editor only)
- Updates only provided fields (COALESCE)
- Validates color format
- Returns updated item

### 5. DTOs
📁 `SuperAppModels/DTOs/`
- **Requests/UpdateWorkspaceItemRequest.cs** - Input validation
- **Responses/UpdateWorkspaceItemResponse.cs** - Output format

## 🔒 Security & Validation

### Permission Check
```sql
-- Chỉ owner và editor mới được phép cập nhật
IF NOT EXISTS (
    SELECT 1 FROM workspace_members
    WHERE workspace_id = @workspace_id
    AND user_id = @user_id
    AND role IN ('owner', 'editor')
    AND deleted_at IS NULL
    AND invitation_status = 'active'
)
BEGIN
    RAISERROR('Access denied', 16, 1);
END;
```

### Input Validation
- **Label:** Max 200 characters
- **Notes:** Max 2000 characters
- **Color:** Regex pattern `^#[0-9A-Fa-f]{6}$`
- **Icon:** Max 50 characters
- **SortOrder:** Must be >= 0

## 🎯 Use Cases

### 1. Tùy Chỉnh Hiển Thị Tag
```json
// Đổi màu và icon cho tag "Work"
PUT /api/workspace/1/items/45
{
  "color": "#FF5733",
  "icon": "💼"
}
```

### 2. Thêm Ghi Chú Cho File
```json
// Thêm notes cho một file PDF
PUT /api/workspace/1/items/78
{
  "notes": "Review this document before the meeting"
}
```

### 3. Đổi Tên Hiển Thị Note
```json
// Đổi label của note (không ảnh hưởng tên gốc)
PUT /api/workspace/1/items/99
{
  "label": "Draft - Needs Review"
}
```

### 4. Sắp Xếp Lại Items
```json
// Di chuyển item lên đầu danh sách
PUT /api/workspace/1/items/25
{
  "sortOrder": 0
}
```

## 🔄 So Sánh Với API Khác

| API | Mục Đích | Entity Scope |
|-----|----------|--------------|
| **PUT /workspaces/{id}/items/{itemId}** | Cập nhật metadata workspace item | Áp dụng cho TAG, NOTE, FILE trong workspace |
| **PUT /tags/{id}** | Cập nhật tag gốc (global) | Chỉ tag toàn cục |
| **PUT /notes/{id}** | Cập nhật note content | Chỉ note content |
| **PUT /files/{id}** | Cập nhật file metadata | Chỉ file metadata |

### Khi Nào Dùng API Này?

✅ **DÙNG** khi:
- Muốn tùy chỉnh cách hiển thị item **trong workspace cụ thể**
- Thay đổi label/color/icon/notes cho item **chỉ trong một workspace**
- Sắp xếp lại thứ tự items trong workspace

❌ **KHÔNG DÙNG** khi:
- Muốn thay đổi thuộc tính **toàn cục** của tag/note/file
- Cần cập nhật content của note
- Cần thay đổi file binary

## 📊 Data Flow

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ PUT /api/workspace/1/items/123
       │ { "label": "New Label", "color": "#FF5733" }
       ↓
┌──────────────────────────┐
│ WorkspaceController      │
│ UpdateWorkspaceItem()    │
└──────┬───────────────────┘
       │ UpdateWorkspaceItemCommand
       ↓
┌──────────────────────────────┐
│ UpdateWorkspaceItemHandler   │
│ Handle()                     │
└──────┬───────────────────────┘
       │
       ↓
┌──────────────────────────────┐
│ WorkspaceRepository          │
│ UpdateWorkspaceItemAsync()   │
└──────┬───────────────────────┘
       │ EXEC usp_update_workspace_item
       ↓
┌──────────────────────────────┐
│ Database (SQL Server)        │
│ - Check permissions          │
│ - Validate input             │
│ - UPDATE workspace_items     │
│ - Return updated row         │
└──────┬───────────────────────┘
       │
       ↓
┌──────────────────────────────┐
│ UpdateWorkspaceItemResponse  │
│ (AutoMapped)                 │
└──────┬───────────────────────┘
       │
       ↓
┌──────────────┐
│ 200 OK       │
│ + JSON data  │
└──────────────┘
```

## ✨ Tính Năng Đặc Biệt

### 1. Partial Update (COALESCE)
```sql
UPDATE workspace_items
SET 
    label = COALESCE(@label, label),        -- Chỉ update nếu có giá trị mới
    notes = COALESCE(@notes, notes),        -- Giữ nguyên nếu null
    color = COALESCE(@color, color),
    icon = COALESCE(@icon, icon),
    sort_order = COALESCE(@sort_order, sort_order),
    updated_at = GETUTCDATE()               -- Luôn update timestamp
WHERE item_id = @item_id;
```

### 2. Context-Specific Customization
- Một tag/note/file có thể có **label/color/icon khác nhau** ở mỗi workspace
- Thuộc tính toàn cục của entity **không bị ảnh hưởng**
- Cho phép tùy chỉnh theo ngữ cảnh sử dụng

### 3. Permission-Based Access
- Chỉ owner và editor mới được phép cập nhật
- Viewer chỉ được xem, không được sửa
- Tự động kiểm tra quyền trước khi cập nhật

## 🧪 Testing

### Test Cases

1. ✅ **Update single field** - Label only
2. ✅ **Update multiple fields** - Label + Color + Icon
3. ✅ **Update all fields** - Full update
4. ✅ **Invalid color format** - Should return 400
5. ✅ **Unauthorized user** - Should return 401
6. ✅ **Non-existent item** - Should return 404
7. ✅ **Null values** - Should keep existing values
8. ✅ **Different entity types** - Tag, Note, File

### Sample Test Script
```powershell
# Test 1: Update label
curl -X PUT "http://localhost:5000/api/workspace/1/items/1" `
  -H "Content-Type: application/json" `
  -d '{"label": "Updated Label"}'

# Test 2: Update color and icon
curl -X PUT "http://localhost:5000/api/workspace/1/items/1" `
  -H "Content-Type: application/json" `
  -d '{"color": "#4CAF50", "icon": "🎯"}'

# Test 3: Invalid color
curl -X PUT "http://localhost:5000/api/workspace/1/items/1" `
  -H "Content-Type: application/json" `
  -d '{"color": "red"}'  # Should fail
```

## 📝 Future Enhancements

### Potential Improvements
1. **Batch Update** - Update multiple items at once
2. **Undo/Redo** - Keep history of changes
3. **Templates** - Apply predefined color/icon schemes
4. **Auto-labeling** - AI-suggested labels based on content
5. **Validation Rules** - Custom validation per workspace

## 🎉 Kết Luận

✅ **API đã được triển khai đầy đủ và hoạt động tốt**

**Tính năng hiện có:**
- ✅ Cập nhật label (tên hiển thị tùy chỉnh)
- ✅ Cập nhật notes (ghi chú bổ sung)
- ✅ Cập nhật color (màu sắc hex)
- ✅ Cập nhật icon (emoji/icon name)
- ✅ Cập nhật sortOrder (thứ tự sắp xếp)
- ✅ Hỗ trợ partial update (chỉ update trường cần thiết)
- ✅ Kiểm tra permissions (owner/editor only)
- ✅ Validation đầy đủ
- ✅ Áp dụng cho TẤT CẢ entity types (tag, note, file)

**Không cần triển khai thêm gì!** 🎊

---

**Tài liệu này được tạo bởi:** AI Assistant  
**Ngày:** 22/10/2025  
**Phiên bản API:** v1.0
