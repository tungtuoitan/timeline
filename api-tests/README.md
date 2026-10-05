# API tests — project / task / comment

**Fullpath:** /api-tests/README.md

Test tích hợp gọi BE thật (SuperApp task #1471), viết theo mẫu `testcase.md` + `node:test` của
#113. Mỗi `flows/NN-*/` có `testcase.md` (bảng case, cột Pass) và `NN-*.test.js`.

## Chạy

1. BE local trỏ **SuperApp-dev** trên `http://localhost:5000` (`dotnet run --project SuperAppAPI`).
   Máy không vào thẳng được `157.66.101.51:1433` → mở tunnel
   `ssh -N -L 14330:127.0.0.1:1433 vps-superapp` và cho BE dùng `Server=127.0.0.1,14330`
   (password `sa` lấy qua `secret run -e ...=vps/sql_server.sa_password`, xem skill `find-credential`).
2. Chạy test (Node ≥ 20, không cần `npm install`):

   ```bash
   node --test --test-concurrency=1 "flows/**/*.test.js"
   ```

- Mỗi lần chạy tự signup user `apitest+<run>-<tag>@test.local` (password ngẫu nhiên, chỉ trong RAM)
  → không đụng data thật. Cuối file: hard-delete task, soft-delete project.
- `SA_API_URL` đổi base URL; mặc định chặn mọi host không phải localhost (tránh chạy nhầm prod).

## Checklist tổng (lần chạy 2026-10-05 trên SuperApp-dev: 66/66 pass)

- [x] 01 Project — tạo (workspace tự tạo, userId từ JWT), validate, update, batch, filter, soft delete/restore
- [x] 02 Task upsert — default, ngày có offset, batch all-or-nothing, description null/""/note, task con, filter
- [x] 03 Task PATCH — chỉ ghi field gửi lên, `clearFields`, validate, đổi project/parent, ngày ISO→ngày user, 1468
- [x] 04 Task hard delete — xoá comment, giữ task con (parent=null), giới hạn 1–200 id, lô lỗi không xoá gì
- [x] 05 Comment — type, filter, occurredAt (bắt buộc offset) + thứ tự, sửa, reply, xoá lan reply (bug message "Comment created" tìm ra & đã sửa)
- [x] 06 Ownership — user B không đọc/sửa/xoá được project/task/comment của user A
