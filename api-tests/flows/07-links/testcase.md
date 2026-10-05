# Test Cases — Flow 07: Link của task / project (#1477)

**Fullpath:** /api-tests/flows/07-links/testcase.md

> 1 link = 1 dòng `dbo.files` (`mimeType = text/x-uri`, url ngoài) nằm trong workspace của
> project như 1 file item (entityType 4). Link task nằm trong folder của task và gắn task qua
> `pro.task_workspace_item`; link project nằm trong folder `Links` ở gốc workspace.
> Code: `LinkController` → `LinkService`; workspace batch/move-cross có thêm ownership + xoá cache tree.
> Test tự động: cột **Test**; kết quả chạy: [RESULTS.md](../../RESULTS.md).

| # | Mô tả case | Kỳ vọng | Test |
|---|---|---|---|
| 1 | Thêm link vào task chưa có folder. | BE tạo folder task (set `folderWorkspaceItemId`), link nằm trong folder đó; `GET links` trả link với `isLink=true`, `taskWorkspaceItemId` có giá trị. | [07-1-task-links](07-1-task-links.test.js) |
| 2 | Link hiện trong cây workspace ngay (không đợi cache 60s). | `tree/v2` có item entityType 4, `data.mimeType = text/x-uri`, `parentId` = folder task. | [07-1-task-links](07-1-task-links.test.js) |
| 3 | Url không hợp lệ (`javascript:`, tương đối, rỗng). | `status=400`, không tạo gì. | [07-1-task-links](07-1-task-links.test.js) |
| 4 | Không truyền name. | Name = host + path của url. | [07-1-task-links](07-1-task-links.test.js) |
| 5 | Gắn item có sẵn (note) của cùng workspace; gắn lại lần 2. | Có trong list (`isLink=false`), không trùng dòng. | [07-1-task-links](07-1-task-links.test.js) |
| 6 | Link tạo bằng workspace (batch create) bên trong folder task. | Hiện trong `GET task links` (không có `taskWorkspaceItemId`). | [07-1-task-links](07-1-task-links.test.js) |
| 7 | Xoá link của task. | Link trong folder task bị soft delete (biến mất khỏi tree); item có sẵn chỉ bị bỏ gắn (vẫn còn trong tree). | [07-1-task-links](07-1-task-links.test.js) |
| 8 | Đổi tên + url link (`PATCH /api/file/{id}`). | Tree + links trả tên/url mới; url sai → 400; file thường không đổi url → 400. | [07-2-project-links-file](07-2-project-links-file.test.js) |
| 9 | Link project. | BE tạo folder `Links` ở gốc (1 lần), link nằm trong đó; xoá → soft delete. | [07-2-project-links-file](07-2-project-links-file.test.js) |
| 10 | Hard delete task có link. | Task, folder, link biến mất khỏi tree. | [07-2-project-links-file](07-2-project-links-file.test.js) |
| 11 | Ownership: B đọc/thêm/xoá link task/project của A, sửa file của A, batch/move-cross vào workspace A. | `status=404` / HTTP 404, dữ liệu A không đổi. | [07-3-ownership](07-3-ownership.test.js) |
