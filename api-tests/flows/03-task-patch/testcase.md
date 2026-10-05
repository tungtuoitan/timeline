# Test Cases — Flow 03: Task partial update (`PATCH /api/task/{id}`)

**Fullpath:** /api-tests/flows/03-task-patch/testcase.md

> PATCH chỉ ghi field khác null; muốn set null phải dùng `clearFields`. Đây là API chính của
> `superapp.py task patch` và các section save trên FE (process, checklist, description, custom tab).
> Code: `TaskService.PatchTaskAsync` (validate + ownership) → `TaskRepository.PatchTaskAsync`.
> Test tự động: [03-task-patch.test.js](03-task-patch.test.js).

| # | Mô tả case | Kỳ vọng | ☐ User review | ☐ AI review | ☐ Pass |
|---|---|---|---|---|---|
| 1 | Patch chỉ `status`. | Status đổi; title/description/checklist/ngày **không đổi**. | ☐ | ✅ | ✅ |
| 2 | Patch nhiều field cùng lúc (title, priority, type, taskType, orderIndex, isMilestone, startDate, endDate). | Tất cả được ghi. | ☐ | ✅ | ✅ |
| 3 | `clearFields`. | | | | |
| 3.1 | ↳ `description`, `startDate`, `endDate`, `parentTaskId`, `checklistJson`. | Các field đó về null. | ☐ | ✅ | ✅ |
| 3.2 | ↳ Field không cho clear (vd `title`). | `status=400` "Cannot clear field(s): title". | ☐ | ✅ | ✅ |
| 4 | Validate. | | | | |
| 4.1 | ↳ `title` rỗng/khoảng trắng. | `status=400` "Title cannot be empty". | ☐ | ✅ | ✅ |
| 4.2 | ↳ `status` rỗng. | `status=400` "Status cannot be empty". | ☐ | ✅ | ✅ |
| 4.3 | ↳ `parentTaskId` = chính nó. | `status=400` "A task cannot be its own parent". | ☐ | ✅ | ✅ |
| 4.4 | ↳ Task id không tồn tại. | HTTP 404. | ☐ | ✅ | ✅ |
| 4.5 | ↳ id = 0. | HTTP 400 "Invalid task ID". | ☐ | ✅ | ✅ |
| 5 | Chuyển task sang project khác (của mình). | `projectId` đổi; response kèm `projectStartDate/EndDate` của project mới. | ☐ | ✅ | ✅ |
| 6 | Gán `parentTaskId`. | Response kèm `parentStartDate/EndDate` của cha. | ☐ | ✅ | ✅ |
| 7 | Ngày dạng ISO cũ có offset (`2026-10-04T17:30:00Z`). | Quy về ngày theo timezone user (Asia/Ho_Chi_Minh) → `2026-10-05`. | ☐ | ✅ | ✅ |
| 8 | *(1468)* Đổi status → `completed` khi checklist còn mục chưa tick. | Cho phép, lưu `completed`. | ☐ | ✅ | ✅ |
| 9 | *(1468)* Patch `processJson` (tick hết step). | Status task **không** tự đổi. | ☐ | ✅ | ✅ |
| 10 | Alias cũ `note` trong PATCH. | Ghi vào `description`. | ☐ | ✅ | ✅ |
