# Test Cases — Flow 01: Project (`/api/project`)

**Fullpath:** /api-tests/flows/01-project/testcase.md

> Case cho API project: `GET /api/project`, `GET /api/project/{id}`, `POST /api/project` (= `/batch`,
> upsert theo lô). Code: `ProjectController` → `ProjectService.UpsertProjectsAsync` →
> `ProjectRepository`. Test tự động: [01-project.test.js](01-project.test.js) (`node:test`, gọi BE
> local trỏ SuperApp-dev). Cột "AI review" = Claude verify bằng đọc code; "User review" = Tung tự
> test trên UI/DB thật; "Pass" = test tự động pass trên DB dev.
>
> **Đánh số 2 cấp:** `X` = case cha, `X.1` = case con (dòng con thụt bằng `↳`).

| # | Mô tả case | Kỳ vọng | ☐ User review | ☐ AI review | ☐ Pass |
|---|---|---|---|---|---|
| 1 | Tạo project mới (`id=0`). | `success=true`, `id>0`, `userId` = user đang gọi (lấy từ JWT, bỏ qua `userId` client gửi). | ☐ | ✅ | ✅ |
| 1.1 | ↳ Workspace tự tạo. | `workspaceId` khác null — BE tự tạo workspace 1:1 cùng tên. | ☐ | ✅ | ✅ |
| 1.2 | ↳ Client gửi `userId` giả. | Bị ghi đè bằng user trong token. | ☐ | ✅ | ✅ |
| 2 | Validate input. | | | | |
| 2.1 | ↳ Thiếu `name`. | HTTP 400 (model validation). | ☐ | ✅ | ✅ |
| 2.2 | ↳ `name` > 255 ký tự. | HTTP 400. | ☐ | ✅ | ✅ |
| 2.3 | ↳ Tạo mới kèm `deletedAt`. | `success=false`, `status=400` "Cannot set deletedAt on a new project". | ☐ | ✅ | ✅ |
| 2.4 | ↳ Body rỗng `[]`. | HTTP 400. | ☐ | ✅ | ✅ |
| 2.5 | ↳ Không có token. | HTTP 401. | ☐ | ✅ | ✅ |
| 3 | Update project (upsert với `id>0`) — đổi name/description/status/startDate/endDate. | GET trả đúng giá trị mới; ngày dạng `YYYY-MM-DD` (không timezone). | ☐ | ✅ | ✅ |
| 4 | Batch upsert nhiều project trong 1 request. | Tạo đủ N project, trả N phần tử. | ☐ | ✅ | ✅ |
| 5 | Filter GET. | | | | |
| 5.1 | ↳ `status=a,b` (CSV). | Chỉ trả project có status trong danh sách. | ☐ | ✅ | ✅ |
| 5.2 | ↳ `ids=1,2`. | Chỉ trả các id đó. | ☐ | ✅ | ✅ |
| 5.3 | ↳ `searchText` (name hoặc description). | Khớp cả theo name và theo description. | ☐ | ✅ | ✅ |
| 5.4 | ↳ Thứ tự. | Mới tạo trước (`createdAt` giảm dần). | ☐ | ✅ | ✅ |
| 6 | `GET /api/project/{id}`. | Trả đúng 1 project. | ☐ | ✅ | ✅ |
| 7 | Soft delete / restore qua upsert `deletedAt`. | | | | |
| 7.1 | ↳ Soft delete. | `deletedAt=null` không thấy; `deletedAt=notNull` thấy. | ☐ | ✅ | ✅ |
| 7.2 | ↳ Restore (`deletedAt: null`). | Thấy lại trong `deletedAt=null`. | ☐ | ✅ | ✅ |
| 8 | Status mới `open`/`planned` (0095). | Lưu & đọc lại được. | ☐ | ✅ | ✅ |
