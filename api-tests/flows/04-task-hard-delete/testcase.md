# Test Cases — Flow 04: Hard delete task (`DELETE /api/task`)

**Fullpath:** /api-tests/flows/04-task-hard-delete/testcase.md

> Xoá vĩnh viễn task + dữ liệu liên quan (comment, checklist history, flow edge/position, folder +
> note workspace, keyword). Dùng bởi `superapp.py task delete`. Code: `TaskService.HardDeleteTasksAsync`.
> Test tự động: [04-task-hard-delete.test.js](04-task-hard-delete.test.js).

| # | Mô tả case | Kỳ vọng | ☐ User review | ☐ AI review | ☐ Pass |
|---|---|---|---|---|---|
| 1 | Xoá 1 task có comment + task con. | Task biến mất hẳn (kể cả `deletedAt=notNull`). | ☐ | ✅ | ✅ |
| 1.1 | ↳ Comment của task. | Bị xoá (GET comment của task → 404 vì task không còn). | ☐ | ✅ | ✅ |
| 1.2 | ↳ Task con. | **Không** bị xoá, `parentTaskId` về null. | ☐ | ✅ | ✅ |
| 2 | Xoá nhiều task 1 lần. | Tất cả biến mất, message "Permanently deleted N task(s)". | ☐ | ✅ | ✅ |
| 3 | `ids` rỗng. | HTTP 400. | ☐ | ✅ | ✅ |
| 4 | > 200 id. | HTTP 400. | ☐ | ✅ | ✅ |
| 5 | Lẫn 1 id không tồn tại. | `status=404`, **không** xoá task hợp lệ nào trong lô. | ☐ | ✅ | ✅ |
