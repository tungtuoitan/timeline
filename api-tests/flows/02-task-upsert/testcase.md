# Test Cases — Flow 02: Task upsert + query (`POST/GET /api/task`)

**Fullpath:** /api-tests/flows/02-task-upsert/testcase.md

> Case cho tạo/sửa task theo lô (`POST /api/task` = `/batch`, full-replace) và đọc/lọc task
> (`GET /api/task`, `GET /api/task/{id}`). Code: `TaskController` → `TaskService.UpsertTasksAsync`
> (ownership guard) → `TaskRepository.UpsertTasksAsync` (1 transaction) / `GetTasksAsync`.
> Test tự động: [02-task-upsert.test.js](02-task-upsert.test.js).

| # | Mô tả case | Kỳ vọng | ☐ User review | ☐ AI review | ☐ Pass |
|---|---|---|---|---|---|
| 1 | Tạo task chỉ với `projectId` + `title`. | Default: `type=task`, `taskType=personal`, `status=open`, `priority=low`, `isMilestone=false`. | ☐ | ✅ | ✅ |
| 1.1 | ↳ `createdAt`/`updatedAt` trả về. | ISO 8601 **có offset** (task #1450 — không còn Fake UTC). | ☐ | ✅ | ✅ |
| 2 | Validate input. | | | | |
| 2.1 | ↳ Thiếu `title`. | HTTP 400. | ☐ | ✅ | ✅ |
| 2.2 | ↳ `title` > 500 ký tự. | HTTP 400. | ☐ | ✅ | ✅ |
| 2.3 | ↳ `projectId` không tồn tại. | `success=false`, `status=404` "Project not found or access denied". | ☐ | ✅ | ✅ |
| 2.4 | ↳ Tạo mới kèm `deletedAt`. | `status=400`. | ☐ | ✅ | ✅ |
| 2.5 | ↳ `id>0` không tồn tại. | `status=404` "Task not found or access denied". | ☐ | ✅ | ✅ |
| 3 | Batch có 1 phần tử lỗi (project không tồn tại). | Cả lô bị từ chối — phần tử hợp lệ cũng **không** được tạo. | ☐ | ✅ | ✅ |
| 4 | Update full (upsert `id>0`). | Các field được ghi đè theo body. | ☐ | ✅ | ✅ |
| 4.1 | ↳ `description` = null/không gửi. | **Giữ nguyên** description cũ (tab FE cũ post thiếu field). | ☐ | ✅ | ✅ |
| 4.2 | ↳ `description` = `""`. | Xoá description (trả `""`). | ☐ | ✅ | ✅ |
| 4.3 | ↳ Alias cũ `note` (0109). | Được ghi vào `description`. | ☐ | ✅ | ✅ |
| 5 | JSON field (`checklistJson`, `processJson`, `customTabsJson`) + `isMilestone`. | Round-trip nguyên vẹn. | ☐ | ✅ | ✅ |
| 6 | Task con (`parentTaskId`). | GET trả `parentStartDate/parentEndDate` của cha và `projectStartDate/projectEndDate` của project. | ☐ | ✅ | ✅ |
| 7 | Soft delete / restore qua upsert `deletedAt`. | `deletedAt=null` ẩn, `notNull` thấy; restore thấy lại. | ☐ | ✅ | ✅ |
| 8 | Filter GET. | | | | |
| 8.1 | ↳ `status` CSV, so khớp **chính xác**. | `status=open` không trả task `reopened`. | ☐ | ✅ | ✅ |
| 8.2 | ↳ `priority` CSV. | Chỉ trả đúng priority. | ☐ | ✅ | ✅ |
| 8.3 | ↳ `type`. | Chỉ trả đúng type. | ☐ | ✅ | ✅ |
| 8.4 | ↳ `searchText` (title hoặc description). | Khớp cả 2. | ☐ | ✅ | ✅ |
| 8.5 | ↳ `projectIds` CSV. | Chỉ task của các project đó. | ☐ | ✅ | ✅ |
| 8.6 | ↳ Thứ tự. | Theo `orderIndex` tăng dần. | ☐ | ✅ | ✅ |
| 9 | `GET /api/task/{id}` id không có. | `success=true`, `data` rỗng. | ☐ | ✅ | ✅ |
| 10 | Ngày `startDate/endDate` gửi `YYYY-MM-DD`. | Lưu & trả đúng chuỗi đó (DateOnly, không lệch múi giờ). | ☐ | ✅ | ✅ |
