# API tests — project / task / comment

**Fullpath:** /api-tests/README.md

Test tích hợp gọi BE thật (SuperApp task #1471). Cấu trúc theo harness của #113 (repo `ai`,
skill `task-testing`), có điều chỉnh: test nằm trong repo code (version cùng BE, gắn CI được),
mỗi file test dùng user riêng (không va chạm data), trạng thái chỉ ghi ở `RESULTS.md` sinh tự động.

## Cấu trúc

```
api-tests/
├── run-tests.js            chạy test (có lọc) + sinh RESULTS.md
├── RESULTS.md              kết quả lần chạy cả bộ gần nhất — SINH TỰ ĐỘNG, không sửa tay
├── _lib/                   harness dùng chung
│   ├── dataManager.js      facade: newSession(tag) -> user test + mọi hàm domain bind sẵn
│   ├── auth.js · project.js · task.js · comment.js   1 file / domain, JSDoc Preconditions/Side-effects
│   ├── cleanupData.js      dọn data của session trong after()
│   ├── verify.js           assert dùng chung (ok, assertFail, assertDenied, ISO_WITH_OFFSET...)
│   ├── http.js · config.js
├── _cases/_TEMPLATE.js     khung 1 file test mới
├── _scripts/cleanup-test-data-by-prefix.sql   dọn TOÀN BỘ data test còn sót trên SuperApp-dev
└── flows/<NN>-<flow>/
    ├── testcase.md         bảng case: # · mô tả · kỳ vọng · file test
    └── <NN>-<group>-<slug>.test.js   1 file = 1 nhóm case cùng hành vi
```

## Chạy

1. BE local trỏ **SuperApp-dev** ở `http://localhost:5000`. Máy không vào thẳng được
   `157.66.101.51:1433` → mở tunnel `ssh -N -L 14331:127.0.0.1:1433 vps-superapp`, cho BE dùng
   `Server=127.0.0.1,14331` với password lấy qua
   `secret run -e ...=vps/sql_server.sa_password` (skill `find-credential` — không ghi pass ra file).
2. Chạy (Node ≥ 20, không cần `npm install`):

   ```bash
   node run-tests.js          # cả bộ -> ghi RESULTS.md
   node run-tests.js 03 05-2  # lọc theo đường dẫn (không ghi RESULTS.md)
   ```

3. Dọn data còn sót (user test, workspace, project đã soft delete, lịch sử temporal — API không
   xoá được những thứ này). Mặc định dry-run + rollback; `APPLY=1` mới xoá:

   ```bash
   secret run -e SQLCMDPASSWORD=vps/sql_server.sa_password -- sqlcmd -S 127.0.0.1,14331 -U sa -C -I -b \
     -d SuperApp-dev -v APPLY=0 -i _scripts/cleanup-test-data-by-prefix.sql
   ```

## Quy ước

- Data test: mọi user có email `apitest+<run>-<tag>@test.local` — đó là "prefix" để dọn; mọi
  data khác đều thuộc các user này nên không bao giờ đụng data thật.
- Tạo và assert đều qua API (như FE / `superapp.py`), không đọc DB — test không dính schema.
- Tên test bắt đầu bằng `Flow<NN>#<case>` khớp cột `#` của `testcase.md`.
- Chỉ tự động hoá hành vi đáng giá: nhiều bước, nhiều nhánh, rule bảo mật/ownership, dễ hồi quy
  âm thầm. Thứ test tay vài giây là thấy thì không cần.
- `config.js` chặn chạy vào host không phải localhost (tránh tạo rác trên prod).
