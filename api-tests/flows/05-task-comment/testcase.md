# Test Cases — Flow 05: Task comment (`/api/taskcomment`)

**Fullpath:** /api-tests/flows/05-task-comment/testcase.md

> Comment của task, gồm các loại `comment | decision | devlog | track` (TungRoot ghi devlog/decision
> qua `superapp.py comment add`). Code: `TaskCommentController` → `TaskCommentService` →
> `TaskCommentRepository`. Test tự động: cột **Test**; kết quả chạy: [RESULTS.md](../../RESULTS.md).

| # | Mô tả case | Kỳ vọng | Test |
|---|---|---|---|
| 1 | Tạo comment không gửi `type`. | `type=comment`, `userId` = user gọi, `occurredAt=null`, `createdAt` có offset. | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 1.1 | ↳ Message trả về khi tạo mới. | "Comment created". *(Bug tìm ra 2026-10-05: trước đây luôn trả "Comment updated" vì message tính sau `SaveChanges` — đã sửa trong `TaskCommentRepository`.)* | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 2 | Tạo comment `devlog`/`decision`/`track`. | Lưu đúng type. | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 2.1 | ↳ Type lạ (`foo`). | `status=400` "Unknown comment type". | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 3 | Content rỗng / khoảng trắng. | HTTP 400 (model validation — `[Required]` coi khoảng trắng là rỗng). | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 4 | GET lọc `type=devlog,decision`. | Chỉ trả 2 loại đó. | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 4.1 | ↳ Filter type lạ. | `status=400`. | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 5 | `occurredAt`. | | | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 5.1 | ↳ Có offset (`...Z`). | Lưu đúng thời điểm (so sánh instant). | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 5.2 | ↳ Không offset (`2026-10-01T09:00:00`). | HTTP 400 (task #1450). | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 5.3 | ↳ Thứ tự GET. | Sắp theo `occurredAt ?? createdAt` tăng dần — comment backdate đứng trước. | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
| 6 | Sửa comment (`id>0`). | | | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 6.1 | ↳ Đổi content, không gửi type/occurredAt. | Content đổi; type + occurredAt **giữ nguyên**. | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 6.2 | ↳ `id` thuộc task khác với `taskId` gửi lên. | `status=404`. | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 6.3 | ↳ Sửa comment đã xoá. | `status=404`. | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 7 | Reply (`parentCommentId`). | | | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 7.1 | ↳ Cha cùng task. | OK. | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 7.2 | ↳ Cha thuộc task khác. | `status=404` "Parent comment not found". | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 8 | Xoá comment cha. | Soft delete cha **và** các reply (không còn trong GET). | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 8.1 | ↳ Xoá id không tồn tại. | `status=404`. | [05-2-edit-reply-delete](05-2-edit-reply-delete.test.js) |
| 9 | GET comment của task không tồn tại. | `status=404`. | [05-1-create-type-filter](05-1-create-type-filter.test.js) |
