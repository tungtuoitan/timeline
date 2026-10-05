# Test Cases — Flow 06: Ownership / cô lập dữ liệu giữa user

**Fullpath:** /api-tests/flows/06-ownership/testcase.md

> User B **không** được đọc/sửa/xoá dữ liệu của user A qua bất kỳ API project/task/comment nào.
> Task thuộc về chủ của project chứa nó (`pro.task` không có `user_id`). Ghi bị chặn trả
> `status=404` "... not found or access denied" (không lộ việc row có tồn tại). Code:
> `OwnershipGuard` + filter `userId` ở các repository. Test tự động: cột **Test**; kết quả chạy: [RESULTS.md](../../RESULTS.md).

| # | Mô tả case | Kỳ vọng | Test |
|---|---|---|---|
| 1 | B đọc project của A (`GET /api/project?ids=`, `GET /api/project/{id}`, `searchText`). | Rỗng. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 2 | B đọc task của A (`projectIds=`, `ids=`, `GET /api/task/{id}`). | Rỗng. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 3 | B sửa project của A (upsert `id` của A). | `status=404`, project A không đổi. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 3.1 | ↳ B tạo project gắn `workspaceId` của A. | `status=404`. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 4 | B tạo task vào project của A. | `status=404`, không có task nào được tạo. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 5 | B upsert task `id` của A (vào project của B). | `status=404`, task A không đổi. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 6 | B tạo task với `parentTaskId` = task của A. | `status=404`. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 7 | B PATCH task của A. | HTTP 404, task A không đổi. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 7.1 | ↳ B PATCH task của mình, chuyển sang project của A. | HTTP 404. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 7.2 | ↳ B PATCH task của mình, gán cha là task của A. | HTTP 404. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 8 | B hard-delete task của A. | `status=404`, task A còn. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 9 | Comment. | | | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 9.1 | ↳ B đọc comment trên task A. | `status=404`. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 9.2 | ↳ B comment vào task A. | `status=404`. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 9.3 | ↳ B sửa comment của A. | `status=404`, nội dung không đổi. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
| 9.4 | ↳ B xoá comment của A. | Không thành công, comment A còn. | [06-1-cross-user-isolation](06-1-cross-user-isolation.test.js) |
