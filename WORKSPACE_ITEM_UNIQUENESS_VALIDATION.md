# Workspace Item Uniqueness Validation

**Created:** October 21, 2025  
**Status:** ✅ Implemented and Verified

## 📋 Overview

Đảm bảo mỗi item trong workspace là **DUY NHẤT** dựa trên tổ hợp:
- `WorkspaceId` + `ParentTagId` + `ChildType` + `ChildId`

Một item không thể được thêm vào workspace nhiều lần với cùng vị trí (parent) và loại.

---

## 🎯 Validation Rules

### ✅ ALLOWED (Valid)
```
Workspace 1:
  ├─ Tag 5 (under root)           ← Thêm lần đầu: OK
  ├─ Tag 3 
  │   └─ Tag 5 (under tag 3)      ← Cùng tag 5 nhưng khác parent: OK
  └─ Note 10 (under root)          ← Khác type: OK
```

### ❌ REJECTED (Duplicate)
```
Workspace 1:
  ├─ Tag 5 (under root)           ← Đã tồn tại
  ├─ Tag 5 (under root)           ← DUPLICATE! Rejected ❌
```

### 📊 Uniqueness Matrix

| Workspace ID | Parent Tag ID | Child Type | Child ID | Result |
|--------------|---------------|------------|----------|---------|
| 1 | null | tag | 5 | ✅ First time - OK |
| 1 | null | tag | 5 | ❌ Duplicate - REJECTED |
| 1 | 3 | tag | 5 | ✅ Different parent - OK |
| 1 | null | note | 5 | ✅ Different type - OK |
| 2 | null | tag | 5 | ✅ Different workspace - OK |

---

## 🏗️ Implementation Architecture

### 1️⃣ Application Layer - Validator (Early Check)

**File:** `SuperApp.Application/Features/Workspaces/Commands/AddItemToWorkspace/AddItemToWorkspaceCommandValidator.cs`

```csharp
// Lines 66-88
RuleFor(x => x)
    .MustAsync(async (command, cancellationToken) =>
    {
        // Skip check if creating new tag (no ChildId)
        if (!command.ChildId.HasValue || command.ChildId.Value <= 0)
            return true;
        
        // Check if this exact item already exists in workspace
        var exists = await _workspaceRepository.ItemExistsAsync(
            command.WorkspaceId,
            command.ParentTagId,
            command.ChildType,
            command.ChildId.Value);
        
        return !exists; // Valid if NOT exists
    })
    .WithMessage(command =>
    {
        return $"Item already exists in workspace: {command.ChildType} with ID {command.ChildId} " +
               $"under parent {(command.ParentTagId.HasValue ? $"tag {command.ParentTagId}" : "root")}";
    });
```

**Purpose:**
- ✅ Catch duplicates **EARLY** before handler execution
- ✅ Return **400 Bad Request** with clear error message
- ✅ Prevent unnecessary processing

**Note:** Skips validation when `ChildId` not provided (creating new tag scenario)

---

### 2️⃣ Repository Layer - Final Safety Check

**File:** `SuperAppDataRepositories/Repositories/WorkspaceRepository.cs`

```csharp
// Lines 167-184
// Check if item already exists in workspace (duplicate prevention)
var exists = await ItemExistsAsync(
    item.WorkspaceId, 
    item.ParentTagId, 
    item.ChildType, 
    item.ChildId);

if (exists)
{
    var parentInfo = item.ParentTagId.HasValue ? $"parent tag {item.ParentTagId}" : "root";
    var message = $"Item already exists in workspace: {item.ChildType} with ID {item.ChildId} " +
                  $"under {parentInfo} in workspace {item.WorkspaceId}";
    
    _logger.LogWarning(
        "Duplicate item rejected: WorkspaceId={WorkspaceId}, ParentTagId={ParentTagId}, " +
        "ChildType={ChildType}, ChildId={ChildId}",
        item.WorkspaceId, item.ParentTagId, item.ChildType, item.ChildId);
    
    throw new ArgumentException(message);
}
```

**Purpose:**
- ✅ **Defense in depth** - Final validation before database insert
- ✅ Handles edge case: When new tag is created in handler, then added to workspace
- ✅ Logs warning for monitoring and debugging

---

### 3️⃣ Helper Method - Database Check

**File:** `SuperAppDataRepositories/Repositories/WorkspaceRepository.cs`

```csharp
// Lines 222-230
public async Task<bool> ItemExistsAsync(int workspaceId, int? parentTagId, string childType, int childId)
{
    return await _context.WorkspaceItems
        .AsNoTracking()
        .AnyAsync(i => 
            i.WorkspaceId == workspaceId &&
            i.ParentTagId == parentTagId &&
            i.ChildType.ToLower() == childType.ToLower() &&
            i.ChildId == childId);
}
```

**Purpose:**
- ✅ Efficient database query (no tracking overhead)
- ✅ Case-insensitive `ChildType` comparison
- ✅ Handles nullable `ParentTagId` correctly

---

## 🔄 Request Flow

```
┌─────────────────────────────────────────────────────────────┐
│ POST /api/workspace/{workspaceId}/items                     │
│ Body: { parentTagId: null, childType: "tag", childId: 5 }   │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ↓
┌─────────────────────────────────────────────────────────────┐
│ 1. Controller: WorkspaceController.AddItemToWorkspace()     │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. ValidationBehavior (MediatR Pipeline)                    │
│    → AddItemToWorkspaceCommandValidator                     │
│    → MustAsync: ItemExistsAsync() check                     │
└─────────────────────┬───────────────────────────────────────┘
                      │
            ┌─────────┴─────────┐
            │                   │
    Exists? YES               NO
            │                   │
            ↓                   ↓
    ┌─────────────┐    ┌────────────────────┐
    │ 400 Bad     │    │ 3. Handler:        │
    │ Request     │    │ AddItemToWorkspace │
    │             │    │ CommandHandler     │
    │ Error:      │    └────────┬───────────┘
    │ "Item       │             │
    │ already     │             ↓
    │ exists..."  │    ┌────────────────────┐
    └─────────────┘    │ 4. Repository:     │
                       │ AddItemToWorkspace │
                       │ Async()            │
                       │                    │
                       │ → Double-check     │
                       │   ItemExistsAsync()│
                       └────────┬───────────┘
                                │
                      ┌─────────┴─────────┐
                      │                   │
              Exists? YES               NO
                      │                   │
                      ↓                   ↓
              ┌──────────────┐   ┌────────────────┐
              │ ArgumentEx   │   │ 5. DbContext   │
              │ thrown       │   │ .Add()         │
              │              │   │ SaveChanges()  │
              │ Caught by    │   └────────┬───────┘
              │ Global       │            │
              │ Exception    │            ↓
              │ Middleware   │   ┌────────────────┐
              │              │   │ 6. Return 201  │
              │ → 400 Bad    │   │ Created        │
              │   Request    │   └────────────────┘
              └──────────────┘
```

---

## 📝 Example Requests & Responses

### ✅ Success - First Time Adding

**Request:**
```http
POST /api/workspace/1/items
Content-Type: application/json

{
  "parentTagId": null,
  "childType": "tag",
  "childId": 5,
  "relationshipType": "contains",
  "sortOrder": 0
}
```

**Response: 201 Created**
```json
{
  "itemId": 123,
  "workspaceId": 1,
  "parentTagId": null,
  "childType": "tag",
  "childId": 5,
  "relationshipType": "contains",
  "sortOrder": 0,
  "createdAt": "2025-10-21T10:30:00Z"
}
```

---

### ❌ Error - Duplicate Item

**Request:**
```http
POST /api/workspace/1/items
Content-Type: application/json

{
  "parentTagId": null,
  "childType": "tag",
  "childId": 5,
  "relationshipType": "contains",
  "sortOrder": 0
}
```

**Response: 400 Bad Request**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "": [
      "Item already exists in workspace: tag with ID 5 under parent root"
    ]
  }
}
```

---

### ✅ Success - Same Tag, Different Parent

**Request:**
```http
POST /api/workspace/1/items
Content-Type: application/json

{
  "parentTagId": 3,      ← Different parent
  "childType": "tag",
  "childId": 5,          ← Same child ID
  "relationshipType": "contains",
  "sortOrder": 0
}
```

**Response: 201 Created**
```json
{
  "itemId": 124,
  "workspaceId": 1,
  "parentTagId": 3,
  "childType": "tag",
  "childId": 5,
  "relationshipType": "contains",
  "sortOrder": 0,
  "createdAt": "2025-10-21T10:35:00Z"
}
```

---

## 🧪 Testing Scenarios

### Test Case 1: Add Same Item Twice to Root
```csharp
// First attempt - should succeed
var request1 = new AddItemToWorkspaceRequest
{
    ParentTagId = null,
    ChildType = "tag",
    ChildId = 5
};
var response1 = await _client.PostAsync($"/api/workspace/1/items", request1);
Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

// Second attempt - should fail with 400
var request2 = new AddItemToWorkspaceRequest
{
    ParentTagId = null,
    ChildType = "tag",
    ChildId = 5
};
var response2 = await _client.PostAsync($"/api/workspace/1/items", request2);
Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
```

### Test Case 2: Same Item, Different Parents
```csharp
// Add tag 5 under root - should succeed
var request1 = new AddItemToWorkspaceRequest
{
    ParentTagId = null,
    ChildType = "tag",
    ChildId = 5
};
var response1 = await _client.PostAsync($"/api/workspace/1/items", request1);
Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

// Add tag 5 under tag 3 - should succeed (different parent)
var request2 = new AddItemToWorkspaceRequest
{
    ParentTagId = 3,
    ChildType = "tag",
    ChildId = 5
};
var response2 = await _client.PostAsync($"/api/workspace/1/items", request2);
Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
```

### Test Case 3: Different Types, Same ID
```csharp
// Add tag with ID 5 - should succeed
var request1 = new AddItemToWorkspaceRequest
{
    ParentTagId = null,
    ChildType = "tag",
    ChildId = 5
};
var response1 = await _client.PostAsync($"/api/workspace/1/items", request1);
Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

// Add note with ID 5 - should succeed (different type)
var request2 = new AddItemToWorkspaceRequest
{
    ParentTagId = null,
    ChildType = "note",
    ChildId = 5
};
var response2 = await _client.PostAsync($"/api/workspace/1/items", request2);
Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
```

---

## 🔍 Database Constraint

Database also enforces uniqueness at the schema level:

```sql
-- workspace_items table
CREATE UNIQUE INDEX UQ_workspace_items_unique
ON workspace_items (workspace_id, parent_tag_id, child_type, child_id)
WHERE deleted_at IS NULL;
```

This provides **triple protection**:
1. ✅ Application Validator (early rejection)
2. ✅ Repository logic (safety check)
3. ✅ Database constraint (final enforcement)

---

## 🎯 Benefits

### 1. Data Integrity
- ✅ No duplicate items in workspace
- ✅ Clean, consistent data model
- ✅ Predictable tree structure

### 2. User Experience
- ✅ Clear error messages
- ✅ Fast validation (early rejection)
- ✅ Prevents confusion from duplicates

### 3. Performance
- ✅ Efficient database query (AsNoTracking)
- ✅ Early validation prevents unnecessary processing
- ✅ Indexed columns for fast lookups

### 4. Maintainability
- ✅ Clear separation of concerns
- ✅ Defense in depth (multiple layers)
- ✅ Well-documented behavior

---

## 📚 Related Files

| File | Purpose | Lines |
|------|---------|-------|
| `WorkspaceController.cs` | API endpoint with documentation | 28-62 |
| `AddItemToWorkspaceCommandValidator.cs` | FluentValidation rules | 66-88 |
| `AddItemToWorkspaceCommandHandler.cs` | Business logic handler | Entire file |
| `WorkspaceRepository.cs` | Data access with validation | 167-230 |
| `IWorkspaceRepository.cs` | Repository interface | 44-52 |

---

## 🔧 Troubleshooting

### Issue: Getting 400 even for valid requests

**Cause:** Item might already exist from previous tests

**Solution:** Check existing items first
```sql
SELECT * FROM workspace_items 
WHERE workspace_id = 1 
AND parent_tag_id IS NULL 
AND child_type = 'tag' 
AND child_id = 5;
```

### Issue: Validation not working

**Cause:** FluentValidation might not be registered

**Solution:** Verify in `DependencyInjection.cs`:
```csharp
services.AddValidatorsFromAssembly(typeof(AddItemToWorkspaceCommandValidator).Assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

---

## ✅ Checklist

- [x] Validator implements `MustAsync` check
- [x] Repository implements `ItemExistsAsync` helper
- [x] Repository validates before insert
- [x] Clear error messages
- [x] Null-safe parent tag handling
- [x] Case-insensitive child type comparison
- [x] Logging for monitoring
- [x] XML documentation in controller
- [x] Performance optimized (AsNoTracking)
- [x] Database constraint as final safety

---

**Last Updated:** October 21, 2025  
**Implemented By:** Development Team  
**Status:** ✅ Production Ready
