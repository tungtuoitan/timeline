# Test Cases — Flow 08: Dashboard + finance (`/api/dashboard/*`, `/api/finance/*`)

**Fullpath:** /api-tests/flows/08-dashboard/testcase.md

> Homepage tiến bộ tổng hợp (TungRoot #1481). Code: `DashboardController` → `DashboardService`
> (+ `DashboardBuckets` thuần, có unit test) → `DashboardRepository`; `FinanceController` →
> `FinanceService` (+ `FinanceCalculator` thuần, có unit test) → `FinanceRepository` (TungRoot #1482: sổ
> giao dịch đa tài sản `fin_transaction` + cache giá `fin_price_cache`). Ngày/tuần theo timezone user (mặc định
> +07:00), tuần bắt đầu thứ Hai. Test tự động: cột **Test**; kết quả chạy: [RESULTS.md](../../RESULTS.md).

| # | Mô tả case | Kỳ vọng | Test |
|---|---|---|---|
| 1 | Activity theo tuần, mặc định devlog/comment/decision. | Đếm đúng theo (tuần, project); comment `track` không tính. | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 1.1 | ↳ Ranh giới tuần theo giờ VN. | CN 23:30 (+07) thuộc tuần trước; T2 00:30 (+07) — dù UTC vẫn là CN — thuộc tuần mới. | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 1.2 | ↳ `types=track`. | Chỉ đếm track. | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 1.3 | ↳ `from > to`, type lạ. | HTTP 400. | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 2 | Habits mặc định. | Chỉ task `type=repeat`; entry gồm `track` + `comment` (không devlog), ngày theo giờ VN. | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 2.1 | ↳ `taskIds=` task thường. | Trả đúng task đó (không cần là repeat). | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 2.2 | ↳ `excludeTaskIds=` tracker. | Tracker đó không có trong kết quả (dùng cho private mode). | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 2.3 | ↳ `taskIds=abc`. | HTTP 400. | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 3 | User B gọi activity/habits. | Không thấy project/task của A, kể cả khi truyền `taskIds` của A. | [08-1-activity-habits](08-1-activity-habits.test.js) |
| 4 | POST lô 6 giao dịch, gửi lại y nguyên. | Lần 1 `inserted=6`; lần 2 `unchanged=6`, list vẫn 6 dòng (chống trùng theo source + externalId). | [08-2-finance](08-2-finance.test.js) |
| 4.1 | ↳ Sửa tay category/note, nguồn gửi lại số tiền mới (+ kind khác). | `updated=1`: amount theo nguồn; kind/category/note giữ như cũ. | [08-2-finance](08-2-finance.test.js) |
| 4.2 | ↳ kind lạ, amount 0, occurredAt không offset, lô rỗng. | HTTP 400, message chỉ đúng dòng lỗi; lô lỗi không chèn gì. | [08-2-finance](08-2-finance.test.js) |
| 5 | GET lọc account / kind / uncategorized / from-to. | Lọc đúng, mới nhất trước; ngày sai / kind lạ -> HTTP 400. | [08-2-finance](08-2-finance.test.js) |
| 6 | Summary 3 ngày, giá asset test đổi ngày 3 (ngày 2 dùng giá ngày 1). | Tài sản ròng [100k, 95k, 112k]; cho mượn tính là tài sản; tháng: thu 7k, chi 5k (invest/debt không phải chi), đầu tư 10k, 1 dòng chưa phân loại. | [08-2-finance](08-2-finance.test.js) |
| 6.1 | ↳ `interval=week`; interval lạ, from > to. | Điểm cuối tuần (CN); tham số sai -> HTTP 400. | [08-2-finance](08-2-finance.test.js) |
| 7 | PUT giá, GET `prices/latest`. | Có (asset, quote, ngày mới nhất); giá <= 0 -> HTTP 400. | [08-2-finance](08-2-finance.test.js) |
| 8 | User B. | Không thấy giao dịch của A; PATCH/DELETE id của A -> HTTP 404; summary rỗng. | [08-2-finance](08-2-finance.test.js) |
| 9 | DELETE giao dịch. | Biến mất khỏi list; xoá lại -> HTTP 404. | [08-2-finance](08-2-finance.test.js) |
