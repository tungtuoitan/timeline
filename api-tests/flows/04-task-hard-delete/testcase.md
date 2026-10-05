# Test Cases — Flow 04: Hard delete task (`DELETE /api/task`)

**Fullpath:** /api-tests/flows/04-task-hard-delete/testcase.md

> Xoá vĩnh viễn task + dữ liệu liên quan (comment, checklist history, flow edge/position, folder +
> note workspace, keyword). Dùng bởi `superapp.py task delete`. Code: `TaskService.HardDeleteTasksAsync`.
> Test tự động: cột **Test**; kết quả chạy: [RESULTS.md](../../RESULTS.md).

| # | Mô tả case | Kỳ vọng | Test |
|---|---|---|---|
| 1 | Xoá 1 task có comment + task con. | Task biến mất hẳn (kể cả `deletedAt=notNull`). | [04-1-hard-delete](04-1-hard-delete.test.js) |
| 1.1 | ↳ Comment của task. | Bị xoá (GET comment của task → 404 vì task không còn). | [04-1-hard-delete](04-1-hard-delete.test.js) |
| 1.2 | ↳ Task con. | **Không** bị xoá, `parentTaskId` về null. | [04-1-hard-delete](04-1-hard-delete.test.js) |
| 2 | Xoá nhiều task 1 lần. | Tất cả biến mất, message "Permanently deleted N task(s)". | [04-1-hard-delete](04-1-hard-delete.test.js) |
| 3 | `ids` rỗng. | HTTP 400. | [04-1-hard-delete](04-1-hard-delete.test.js) |
| 4 | > 200 id. | HTTP 400. | [04-1-hard-delete](04-1-hard-delete.test.js) |
| 5 | Lẫn 1 id không tồn tại. | `status=404`, **không** xoá task hợp lệ nào trong lô. | [04-1-hard-delete](04-1-hard-delete.test.js) |
| 6 | Xoá task có comment kèm reply (cả reply của comment đã soft delete). | Xoá thành công. *(Bug tìm ra 2026-10-05: từng lỗi 500 `FK_task_comment_parent` vì EF bỏ qua việc gỡ parent khi entity đã Deleted — đã sửa trong `TaskService.HardDeleteTasksAsync`.)* | [04-1-hard-delete](04-1-hard-delete.test.js) |
