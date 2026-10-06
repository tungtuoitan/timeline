# Test Cases — Flow 09: TOTP + API riêng tư (`/api/security/totp/*`, TungRoot #1489)

**Fullpath:** /api-tests/flows/09-totp/testcase.md

> Mở full mode homepage cần mã TOTP. Code: `TotpController` → `TotpService` (+ `Totp`, `UnlockToken` thuần —
> unit test theo vector RFC 6238 ở `SuperAppServices.Tests/TotpTests.cs`) → `UserTotpRepository`
> (`auth.user_totp`). API riêng tư: `GET /api/dashboard/habits` bỏ task `isSensitive` và
> `GET /api/finance/summary` trả 403 khi không có header `X-Unlock-Token` hợp lệ.
> Test tự động: cột **Test**; kết quả chạy: [RESULTS.md](../../RESULTS.md).

| # | Mô tả case | Kỳ vọng | Test |
|---|---|---|---|
| 1 | User mới: status, unlock khi chưa bật. | `enabled=false`; unlock → HTTP 404. | [09-1-setup-unlock](09-1-setup-unlock.test.js) |
| 2 | Setup → confirm sai → confirm đúng. | Setup trả `otpauth://` + secret; sai → 400; đúng → vé mở khoá, `enabled=true`. | [09-1-setup-unlock](09-1-setup-unlock.test.js) |
| 2.1 | ↳ Setup lại khi đã bật. | HTTP 409. | [09-1-setup-unlock](09-1-setup-unlock.test.js) |
| 3 | Unlock bằng mã đã dùng để confirm. | HTTP 400 (mã chỉ dùng 1 lần). | [09-1-setup-unlock](09-1-setup-unlock.test.js) |
| 3.1 | ↳ Unlock bằng mã bước kế tiếp. | Vé mới. | [09-1-setup-unlock](09-1-setup-unlock.test.js) |
| 4 | Sai 3 lần liên tiếp. | Lần 1–2: 400; lần 3: 423 + `lockedUntil` ≈ 10 phút; đang khoá thì mã đúng cũng 423. | [09-2-lock](09-2-lock.test.js) |
| 5 | Habits với tracker `isSensitive`. | Không vé: không có tracker đó; vé hợp lệ: có; vé của user khác / vé rác: không. | [09-3-private-apis](09-3-private-apis.test.js) |
| 5.1 | ↳ PATCH `isSensitive`, rồi upsert task (full replace). | Giữ `isSensitive=true` (upsert không xoá cờ). | [09-3-private-apis](09-3-private-apis.test.js) |
| 6 | Finance summary. | Không vé → HTTP 403; có vé → 200. | [09-3-private-apis](09-3-private-apis.test.js) |
| 7 | Tắt TOTP bằng mã đúng. | `enabled=false`; unlock → 404. | [09-1-setup-unlock](09-1-setup-unlock.test.js) |
