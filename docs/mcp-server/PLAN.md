# MCP server cho SuperApp — plan v15

> Task SuperApp #1734. Bản canonical của plan nằm ở file này (trong repo); mô tả task trên SuperApp là bản sao. Sàn tranh luận: `debates/2026-10-30-mcp-server.md` (A = Claude Sonnet, C = GPT/Codex đã nghỉ từ Vòng 24, B = Claude Fable từ Vòng 25; Tung quyết). v15 = v14 + Vòng 27 của B (4 sửa nhỏ); v14 = v13 + Vòng 25 của B (Q21–Q27 là mặc định A+B, Tung chưa trả lời — đổi được); spec MCP 2026-07-28 và tài liệu Cloudflare AOP đã được A kiểm trực tiếp.

## Mục tiêu

Dùng SuperApp (task, devlog, plan) từ Claude trên **iPhone (iOS 18.5)** và Claude Code mà không cần máy ở nhà bật hay session remote: một **MCP server** thêm vào tài khoản Claude như custom connector. Bảo mật tối đa ngay từ đầu.

## Quyết định đã chốt (Tung, 30/10)

| # | Quyết định | Ghi chú |
|---|---|---|
| Q1 | **Bản đầu: xem + ghi devlog** (`add_devlog` chỉ tạo mới). Không có `tick_checklist`, không xoá, không sửa. | A và C đều đề nghị chỉ-đọc; Tung chọn thêm devlog → rủi ro prompt injection đường ghi **được chấp nhận có điều kiện** (xem mục Injection). |
| Q2 | **MCP chạy cùng process ASP.NET** (MCP C# SDK), **không** service key / unix socket / tiến trình Python riêng. **Một đường duy nhất:** tool → `IMcpFacade` (lớp duy nhất tool được gọi) → domain service / `OwnershipGuard` → repository; facade **gọi** domain service, không copy SQL, không đi thẳng DbContext. | Chấp nhận giới hạn: MCP bị chiếm (RCE) = chiếm cả app. Xem lại nếu mở thêm tool ghi. |
| Q3 | **Authorization server (OAuth) trong backend, dùng thư viện OAuth cho .NET** (ứng viên OpenIddict) — chỉ nếu spike đạt 4 điều kiện: hỗ trợ `resource` (RFC 8707); consent tuỳ biến; đăng ký sẵn 2 client, tắt DCR; bảng token tách riêng. **Nếu không đạt: DỪNG và đổi thư viện/giải pháp; KHÔNG tự viết AS ở v1 (Q9, Tung chốt).** | C5-1: test race không phủ mix-up/code substitution/PKCE downgrade. |
| Q4 | **Đòi TOTP** khi cấp quyền (sau khi Google `sub` khớp) — gọi **service verify nội bộ**, giữ replay/lockout; không gỡ `[Authorize]` của `TotpController`. Recovery chỉ qua SSH/runbook. | |
| Q5 | **Task nhạy cảm (`is_sensitive`) ĐƯỢC đọc qua MCP.** | Tung chọn khác đề nghị của A. **Chấp nhận giới hạn**: Claude có thể đọc rồi (qua connector khác trong cùng chat) gửi ra ngoài. Giảm nhẹ: finance và project nội bộ vẫn **ngoài phạm vi**; kill switch; hướng dẫn không bật connector gửi dữ liệu ra ngoài khi dùng SuperApp. |
| Q6 | **Bước −1 (đóng cửa hạ tầng) làm TRƯỚC mọi thứ.** | Phần chặn ở bảng điều khiển nhà cung cấp VPS: Tung tự làm. |
| Q7 | **Không thêm bảo vệ riêng cho lần ghi devlog ở v1** (không `review_state`, không duyệt trước khi lưu). | Tung: "Tạm thời bỏ qua, vì chỉ có tôi vào dc thôi mà". **CHẤP NHẬN GIỚI HẠN.** Lưu ý ghi lại: rủi ro không phải "ai khác đăng nhập" mà là nội dung độc đi qua Claude (connector khác) rồi gọi `add_devlog`; "luôn hỏi" chỉ là cài đặt phía client, **server không chứng minh được consent**. Giữ `origin=mcp` (provenance, rẻ); hiển thị trích dẫn ở FE/CLI chỉ làm khi tiện. Xem lại khi mở thêm tool ghi hoặc khi bật connector có thể gửi dữ liệu ra ngoài. |
| Q8 | **Đọc đầy đủ cả loạt** (kể cả mô tả task nhạy cảm) trong `list_tasks`. | Tung chọn khác đề nghị của A và C (list tối thiểu). **CHẤP NHẬN GIỚI HẠN**: một lệnh có thể lấy nhiều dữ liệu. Giữ **giới hạn kỹ thuật cố định** (byte tối đa/response, page size, số lần gọi/phút) — không đổi quyết định về nội dung, chỉ chặn quá cỡ/DoS. Comments chưa có ở v1. Số cụ thể: Q11. **"Đọc đầy đủ" nghĩa là trong giới hạn mỗi task (Q11/Q16): phần mô tả vượt cap KHÔNG đọc được qua MCP ở v1; không tài liệu nào được hứa đọc hết.** |
| Q9 | Thư viện OAuth không đạt spike → **dừng, đổi giải pháp; không tự viết v1.** | Tung: "Dừng, đổi giải pháp khác". |
| Q10 | **Giữ `get_plan` ở bản đầu, định nghĩa cứng:** không input; trả mô tả của **một project "plan" cố định lấy từ cấu hình** (hiện là project #23; client không chọn được id), HTML→văn bản, cắt **64 KB** kèm cờ `truncated`, qua `OwnershipGuard`. | Tung: "Giữ, chỉ đọc đúng plan cố định (Recommended)". C8-10. |
| Q11 | **Giới hạn đọc (đã nâng theo Q14): mỗi trang `list_tasks` tối đa 25 task hoặc 256 KiB (cái nào tới trước); tiếp bằng con trỏ (cursor); tối đa 6 lần gọi danh sách mỗi phút.** Tính theo **byte UTF-8 của JSON CUỐI CÙNG sau escape (cả envelope)** — không cộng độ dài chuỗi thô (`\u0000` nở 6 lần khi escape). **Mỗi mô tả task tối đa 64 KiB** (quá thì cắt kèm `truncated:true`); `get_task(id)` envelope ≤ 128 KiB; **cursor luôn tiến**, JSON luôn hợp lệ. | Tung (Q14) "Tăng giới hạn lên", rồi (Q17) chốt **"256 KB/trang, 64 KB/mô tả (Recommended)"**. Trade-off đã nói với Tung: 256 KiB × 6 lần/phút ≈ 1,5 MiB/phút (so ≈ 0,4 MiB nếu giữ 64 KiB). Đọc hết qua cursor trong giới hạn từng task. C8-8, C8-10, C15-8. |
| Q13 | **Nhất quán yếu giữa các trang:** không lặp item; task thêm/khôi phục giữa hai lần gọi có thể phải list lại từ đầu; không làm snapshot cứng. | Mặc định theo đề nghị của C (C11-10); Tung không bị hỏi vì là chi tiết kỹ thuật. |
| Q14 | **Nâng giới hạn đọc** so với mặc định 25 task/64 KiB: **trang tối đa 25 task/256 KiB, mỗi mô tả 64 KiB** (xem Q11). | Tung: "Tăng giới hạn lên". **CHẤP NHẬN GIỚI HẠN**: một lệnh lấy được nhiều dữ liệu hơn; cap vẫn chặn quá cỡ/DoS. |
| Q15 | **Quota `add_devlog` v1: 50 lần/ngày, 16 KiB/lần, 1 lần/10 giây; vượt thì từ chối (không xếp hàng); "16 KiB" = **byte UTF-8**; quota tính theo devlog ĐÃ COMMIT: **quota = số devlog ĐÃ COMMIT trong ngày:** domain operation `AddMcpDevlog` validate (auth/schema/size/rate/ownership) rồi, trong CÙNG một transaction, tăng bộ đếm bằng UPDATE có điều kiện `count < 50` và insert có điều kiện; lỗi/reject/rollback (kể cả task bị xoá giữa chừng) **không tốn quota**; bộ đếm lưu bền, sống qua restart; đây KHÔNG phải distributed transaction/outbox; audit không ghi nội dung và ghi riêng kết quả reject/fail/commit; không dựng distributed transaction/outbox để hoàn slot (C15-10, C15-11).** | Tung: "Rộng hơn: 50 lần/ngày, 16 KB/lần". **CHẤP NHẬN GIỚI HẠN**: nội dung độc ghi được nhiều hơn (so với đề nghị 10/ngày, 4 KiB). Khoảng cách 1 lần/10 giây giữ nguyên (Tung không đổi). |
| Q16 | **Phần mô tả task vượt cap 64 KiB KHÔNG đọc được qua MCP ở v1** (không có tool đọc theo đoạn); app SuperApp vẫn xem đủ. Không thêm chunk tool trừ khi Tung thật sự cần. | Tung: "Chấp nhận không đọc phần vượt (Recommended)". C13-10. Test: mô tả 100 KiB → JSON ≤ giới hạn, `truncated=true`, tài liệu không hứa đọc hết. |
| Q17 | **Số giới hạn đọc: 256 KiB/trang, 64 KiB/mô tả, 128 KiB cho `get_task`.** | Tung: "256 KB/trang, 64 KB/mô tả (Recommended)". C15-8. |
| Q18 | **`list_tasks` chỉ trả: metadata task + description. KHÔNG trả `note` (alias lặp), `checklistJson`, `processJson`, `customTabsJson`.** Muốn thêm các JSON đó phải có cap riêng ở đợt sau. | Tung: "Chỉ thông tin task + mô tả (Recommended)". C15-9. |
| Q19 | **Quota devlog tính theo devlog ĐÃ COMMIT:** input sai / ownership fail / task bị xoá giữa chừng / rollback đều KHÔNG tốn quota (một transaction duy nhất trong `AddMcpDevlog`, không còn khoảng hở giữa kiểm tra và ghi); audit **commit** nằm CÙNG transaction (mỗi devlog commit có đúng một audit commit, kể cả khi process bị kill ngay sau commit); audit reject/fail ghi **best-effort** kèm request-id (Q20). | **Mặc định do A và C cùng đề nghị; Tung chưa trả lời** (câu hỏi bị phiên ngắt) → mặc định, **đổi được**. Thay thế bản "reserve rồi lỗi vẫn tính" của v11 vì bản đó có TOCTOU (C19-8). Chống flood do rate limit 1 lần/10 giây, không do quota. Test (T2): xen kẽ hard-delete/add 100 lần không ghi mồ côi/ghi chéo; counter và audit khớp outcome. |
| Q20 | **Độ bền audit lần ghi thành công:** audit **commit** nằm cùng transaction với quota + devlog; audit reject/fail best-effort kèm request-id. | **Mặc định do A và C cùng đề nghị; Tung chưa trả lời** → đổi được (phương án khác: audit toàn bộ best-effort, chấp nhận khe hổng khi crash). C21-10. Test (T2): kill process ở khoảng sau insert/trước response 100 lần → mọi devlog commit có đúng một audit commit. |
| Q21 | **Audit commit lỗi → rollback cả devlog + quota (fail-closed).** | Hệ quả tất yếu của một transaction (B25: C23-8). Mặc định. |
| Q22 | **Client retry sau commit: chấp nhận at-least-once**, không thêm idempotency key/bảng. Cooldown 1 lần/10 giây theo user+tool (in-memory) đã chặn retry tức thì; devlog kép (nếu có) hiện rõ trên app và xoá được. | B25 (C23-10 bác cho v1): spec 2026-07-28 không hứa client giữ id ổn định. Mặc định. |
| Q23 | **KHÔNG xoá URL khỏi nội dung devlog.** | C23-12: strip URL không chặn injection bằng ngôn ngữ tự nhiên và làm hỏng devlog hợp lệ (devlog của chính repo toàn hash/URL). A, B, C cùng đồng ý. |
| Q24 | **`SuperAppUserId` cấu hình sai/không active lúc khởi động → chỉ TẮT phần MCP (không map `/mcp`, `/mcp-oauth`; log lỗi; `/api/health` báo `mcp: disabled`), app vẫn chạy.** Thêm cờ `Mcp:Enabled` mặc định `false` (deploy code trước, bật sau). | B25-1: "không khởi động" thì một tính năng phụ hạ cả FE/CLI/SignalR. Mặc định. |
| Q25 | **AOP/khoá origin (Bước −1b) KHÔNG phải điều kiện tiên quyết để code MCP**; làm song song, xong trước Bước 4 (lên VPS). Điều kiện tiên quyết chỉ gồm Bước −1a (đóng cổng DB). | B25 phán quyết 1(b)(d): AOP chỉ né WAF/rate-limit Cloudflare, không né auth. Mặc định. |
| Q26 | **Mức test bắt buộc trước Bước 5 = bậc T1** (xem mục Test). T2 trước khi mở tool ghi thứ hai; T3 bỏ. | B25-4: repo chỉ có 7 file test (TotpTests 6 test method (5 `[Fact]` + 1 `[Theory]`)), chưa có harness DB/fault-injection. Mặc định. |
| Q27 | **Hạn tuyệt đối refresh = 90 ngày** (một người dùng, có kill switch), chốt theo transcript 0b. | B25-5: 30 ngày nghĩa là cứ 30 ngày Tung phải Google + TOTP lại trên từng thiết bị. **Cần Tung xác nhận** (đổi được). |

Ngoài ra: **một danh tính** dùng MCP (Tung), ghim bằng Google `sub` + `email_verified`; SuperApp vẫn cho đăng ký tự do (product), siết lại chỉ khi tiện.

## Dữ kiện đã kiểm (30/10)

- **Host chính thức `www.tungle.uk`** (`tungle.uk` 301 → `www`), sau **Cloudflare**; nginx có `real_ip_header CF-Connecting-IP` + dải IP Cloudflare.
- **Kestrel chỉ nghe `127.0.0.1:5000`**; `ASPNETCORE_URLS=http://localhost:5000`; chạy `www-data`.
- **`ufw` tắt, iptables INPUT ACCEPT**; **SQL Server nghe `0.0.0.0:1433`** (chưa xác minh từ ngoài — Tung kiểm).
- **nginx có `listen 80 default_server; server_name _;` phục vụ `/var/www/SuperApp/build` và proxy `/api` → `127.0.0.1:5000`** → gọi thẳng `http://<IP VPS>/api/...` vào backend không qua Cloudflare; CORS còn cho `http://157.66.101.51` (`Startup.cs:131`).
- Backend hiện: JWT HS256 `Jwt:Key`, `ValidateIssuer/Audience=false` (`Startup.cs:60-71`); **không có `UseForwardedHeaders`**; `UseHttpsRedirection:286`, `UseRateLimiter:300`; rate limiter "login" chỉ gắn 3 endpoint (`AuthController.cs:68,121,158`), `refresh` không có; `ClientIp()` tin `X-Forwarded-For` thô (`AuthController.cs:49-54`); `TotpController` có `[Authorize]`; `TaskComment` chưa có trường nguồn.
- iPhone: iOS 18.5. Chưa biết app Claude iOS có custom connector / OAuth đầy đủ không → Bước 0.

## Kiến trúc (cùng process)

```
Claude (iPhone / Claude Code)
   │ (1) OAuth 2.1 + PKCE(S256) với BACKEND (Google → allowlist `sub` → TOTP → consent scope)
   │     → ACCESS TOKEN JWT (ES256/RS256, aud = https://www.tungle.uk/mcp, sub = Google sub, ≤15') + REFRESH (bảng riêng)
   ▼
Cloudflare → nginx → ASP.NET (cùng process):
   /mcp           MCP C# SDK, scheme `Mcp` (chỉ chấp nhận token aud=MCP), scope kiểm TRONG TỪNG TOOL
   /mcp-oauth/*   authorization server = thư viện OAuth đã chọn qua spike 0b (Q3/Q9); KHÔNG có nhánh tự viết
   │
   ▼ `IMcpFacade` (hẹp): ListTasks (query repository RIÊNG: `WHERE ownership AND Id > lastId ORDER BY Id`, lấy N+1 theo ngân sách byte; ownership nằm trong SQL; **không** dùng `GetTasks` cũ vì nó sắp `OrderIndex, CreatedAt` và nạp toàn bộ danh sách — `TaskRepository.cs:102-106,152`; weak consistency, Q13), GetTask, GetPlan (cố định), AddDevlog — DTO tối giản. **Một đường duy nhất:** tool → facade → domain service/`OwnershipGuard` → repository; **ngoại lệ có tên** (không đi qua domain service): query đọc riêng của `list_tasks`/`get_task`/`get_plan`; **ghi (`add_devlog`) luôn qua MỘT domain operation `AddMcpDevlog` (validate một lần; reserve quota + conditional insert trong CÙNG transaction DB; facade KHÔNG tự gọi guard/DbContext)**
   ▼
Database
```

- Scheme `Mcp` **ngoài chính sách mặc định**; token MCP không dùng được trên API thường và ngược lại (test cả hai chiều).
- **Không** tạo `McpController` nội bộ song song với facade (chọn một đường: facade).
- Khoá ký **bất đối xứng riêng** cho token MCP (khác `Jwt:Key`); `kid`; xoay được.

## Ranh giới tin cậy / threat model (ghi thẳng, đừng thổi phồng)

- **MCP cùng process = RCE trong MCP/SDK đọc được connection string và chiếm cả app.** `IMcpFacade` là ranh giới **quyền và dữ liệu**, **không** phải sandbox OS; không được viết "cách ly" ở bất kỳ đâu.
- **Không tuyên bố cách ly tương đương tiến trình riêng.** Chỉ xem lại cách ly process khi mở thêm tool ghi (C8-3).
- `origin=mcp` và audit log là **provenance**, không phải biện pháp consent/chặn (C8-4); mọi câu trong plan đọc được như "đã chặn" phải được rà lại.
- Facade **gọi domain service / `OwnershipGuard`** bằng request do server dựng (`Id=0`, type `devlog`, không parent) — **không** copy SQL, không đi thẳng DbContext; SuperApp là sản phẩm nhiều user nên `add_devlog(taskId của user khác)` phải bị từ chối (xem `TaskCommentService.cs:66-71`). Test với hai user thật: list/get/write không tiết lộ sự tồn tại và không ghi chéo.
- **Adapter facade — contract kết quả (C13-11):** domain service trả **thất bại như dữ liệu**, không ném lỗi (`TaskCommentController.cs:53-64` luôn `Ok(response)`; `OwnershipGuard.Denied` = `Success=false, Status=404`). Facade **bắt buộc kiểm `Success`/`Status`**: 404 ownership → một lỗi chung **không lộ sự tồn tại**; 4xx khác → lỗi input không hợp lệ; 5xx/exception → thông báo chung, **không trả `ex.Message`**. Quota = devlog **đã commit** (cùng transaction với insert trong `AddMcpDevlog` — Q15/Q19; không còn pre-check ownership ở facade rồi service kiểm lại, vì có TOCTOU: task bị hard-delete giữa hai lần kiểm — C19-8); audit **commit cùng transaction** (Q20); reject/fail ghi audit best-effort kèm request-id. Test hai user: `add_devlog(taskId user khác)` không ghi, không báo success, quota/audit đúng.
- **`AddMcpDevlog` sở hữu transaction ở tầng repository (C21-8):** `TaskCommentRepository.UpsertCommentAsync` hiện tự mở transaction và `catch (Exception)` rồi trả `ResultOptions` (`TaskCommentRepository.cs:92-160`) nên **không thể** bọc nó vào một transaction ngoài; không gọi lại repository upsert cũ. Thêm **operation repository riêng**: ownership có điều kiện + `UPDATE counter … WHERE count < 50` + insert + audit commit + commit, tất cả trong MỘT transaction. Test (T2): 100 add/hard-delete song song + fault injection tại từng `SaveChanges`/`Commit` → `counter == số devlog commit`, không orphan, không ghi chéo user.
- **DTO tường minh + projection SQL (C15-9, Q18):** list không có `note`/`checklistJson`/`processJson`/`customTabsJson` (`ProTask.cs:29-54`: các JSON không giới hạn, `LegacyNote` lặp `Description`); **không serialize `ProTask`**. Repository riêng chỉ lấy các cột cần, **mỗi chuỗi đọc tối đa N ký tự bằng `LEFT(col, N)` ngay trong SQL với N = hằng cố định (64 Ki ký tự)**, rồi app cắt đúng byte UTF-8 và bounded-serialize — `LEFT` đếm KÝ TỰ chứ không phải byte nên chỉ là cận worst-case (không tăng theo kích thước DB); áp dụng cả `get_plan` (mô tả project, không được materialize đủ rồi mới cắt) (C17-10); `get_task` cap **từ lúc query cho mọi chuỗi**. Test: schema không có alias/trường thừa, wire ≤ cap, cursor tiến (T1); working set không tăng theo kích thước DB khi trường rất lớn (T2, không dùng ma trận 20 MiB).

## Công cụ v1

`list_tasks` (trang 25 task/256 KiB, mô tả ≤ 64 KiB, cursor, ≤ 6 lần/phút), `get_task(id)` (envelope ≤ 128 KiB, mô tả ≤ 64 KiB, `truncated`), `get_plan` (Q10: mô tả project plan cố định, ≤ 64 KB) — đọc; `add_devlog` (ghi, **chỉ tạo mới**, không nhận id, cắt độ dài, KHÔNG xoá URL (C23-12, Q23), hạn mức theo Q15 ở backend, gắn `origin = mcp`).
Không có: xoá, sửa task, tick checklist, comment khác, project, finance, SQL, gọi API tuỳ ý.

## Bảo mật

**OAuth / xác thực**
- S256 only. **`resource` (spec MCP 2026-07-28 — A đã đọc trực tiếp: "MUST be included in both authorization requests and token requests"):** ở **authorize và đổi code**, thiếu/sai `resource` ≠ `mcp_resource` → `invalid_target`; nhánh tương thích legacy nếu cần thì **theo revision**, không theo "client có gửi hay không". **Refresh (C11-9, chốt):** với grant của client **current** (revision 2026-07-28), refresh thiếu hoặc sai `resource` → `invalid_target`; chỉ khi **0b chứng minh một client/legacy grant cụ thể** cần thì cho **fallback về `resource` đã gắn với grant**, ghi rõ theo client và không đổi `aud`. **Hai hằng riêng, không trộn:** `mcp_resource` (URL MCP, ví dụ `https://www.tungle.uk/mcp`; khớp PRM `resource` và `aud`) và `authorization_server_issuer` (khớp metadata `issuer` và `iss` token); exact-match, không normalize; contract test tráo hai giá trị phải fail.
- **Ghim danh tính bằng cặp `{AllowedGoogleSub → SuperAppUserId}` trong cấu hình** (không chỉ email): AS **không** gọi `GetOrCreateGoogleUserAsync` (tra/tạo user theo email, `AuthService.cs:433`); đổi email không đổi user; khi khởi động xác minh `SuperAppUserId` còn tồn tại và active, sai thì **chỉ TẮT phần MCP** (không map `/mcp`, `/mcp-oauth`; log lỗi; `/api/health` báo `mcp: disabled`), app vẫn chạy (B25-1, Q24); `sub` lạ bị chặn **trước** bước TOTP (TOTP nhận `int userId`, `TotpService.cs:65`).
- **Kiểm `sub → userId → active` ở: authorize, MỖI refresh, và MỖI lần gọi tool `/mcp`** (không cache; lưu lượng một người dùng). Disable user không restart → authorize/refresh/tool fail ngay (C8-5).
- **Phân tách scheme bằng policy, không bằng tên scheme:** policy `McpOnly` chỉ chấp nhận scheme `Mcp` + `aud` + scope + `sub`; API thường chỉ policy/scheme hiện tại; không dùng policy-selector theo token tự khai. Test ma trận 4 ca `{endpoint MCP/API} × {token MCP/app}` chỉ đúng hai đường hợp lệ; test kiến trúc: endpoint mới thiếu policy phải làm test fail. (`[Authorize]` trần chạy scheme mặc định JWT thường — `Startup.cs:59-72`.)
- Metadata: `/.well-known/oauth-authorization-server` (kèm `code_challenge_methods_supported:["S256"]`; chỉ quảng bá `authorization_response_iss_parameter_supported:true` nếu thật sự trả `iss`), `/.well-known/oauth-protected-resource` (đường dẫn có hậu tố theo RFC 9728/8414); nginx route đúng; MCP trả `401 WWW-Authenticate` đúng chuẩn. (Spec 2026-07-28 **đã được A kiểm trực tiếp**; vẫn có contract test exact-string ở Bước 0b cho từng client.)
- Redirect URI: allowlist `https://claude.ai/api/mcp/auth_callback` (+ claude.com) và loopback mọi cổng (RFC 8252) cho Claude Code; không redirect trước khi validate `client_id` + `redirect_uri`. **DCR tắt**, 2 client đăng ký sẵn.
- **Đăng ký client (spec: CIMD SHOULD, DCR deprecated, pre-registration được phép):** spike 0b đo từng client Claude dùng cơ chế nào. Nếu CIMD: chỉ chấp nhận `client_id` thuộc **allowlist URL chính xác**, chặn private IP/redirect khi fetch (SSRF), timeout + giới hạn kích thước, cache có hạn, `redirect_uris` trong metadata phải nằm trong allowlist redirect; **không bật CIMD chung** (A9-1).
- Chống login CSRF/mix-up: cookie `__Host-` SameSite=Lax gắn yêu cầu authorize, bắt buộc ở callback Google + POST consent; token CSRF; `frame-ancestors 'none'` + `X-Frame-Options` (đăng ký `SecurityHeadersMiddleware` — hiện có file nhưng chưa đăng ký); luôn hiện consent.
- Đường authorize MCP riêng: chỉ xin `openid email`; **không đụng** `GoogleRefreshToken`/Drive (`AuthService.cs:153`); allowlist `sub` **trước** mọi tra cứu/tạo user; không trả lỗi Google nguyên văn.
- Refresh MCP ở **bảng riêng**, mỗi grant một gia đình, gắn `client_id` + `resource`, xoay mỗi lần dùng, dùng lại = thu hồi **cả gia đình** (không dùng `RefreshTokenAsync` hiện có), hạn tuyệt đối 90 ngày (Q27; chốt theo transcript 0b). Redeem code / refresh phải **nguyên tử** (cùng transaction) + test song song.
- Access token ≤ 15'; ký sai / `aud` / `iss` / `typ` / `alg` sai (kể cả `none`, HS256) bị từ chối.
- TOTP: AS gọi service verify nội bộ sau khi `sub` khớp; test không JWT vẫn hoàn tất flow, sai/replay bị khoá, `sub` lạ không tới bước TOTP.
- **TOTP phải an toàn khi song song (C19-9, C21-9, C23-5, B25):** `TotpService.CheckAsync` hiện đọc→sửa `FailedCount`/`LastUsedStep`→`SaveAsync` không nguyên tử, `UserTotp` không có rowversion, và `Confirm`/`Unlock`/`Disable` đều `GetAsync` **trước** khi gọi `CheckAsync` (`TotpService.cs:53-55,67-69,74-76`). **Cách chọn cho v1:** trong MỘT transaction (qua execution strategy), lấy **khoá ứng dụng theo user** bằng `sp_getapplock @Resource = 'totp:<userId>', @LockMode = 'Exclusive', @LockOwner = 'Transaction'` ngay đầu delegate, rồi load → verify → callback → save → commit, cho cả ba method. Lý do: `SERIALIZABLE` với mẫu đọc-rồi-ghi trên cùng một hàng gây deadlock chắc chắn khi hai request song song (hai S-lock rồi nâng lên X → lỗi 1205); applock theo user tuần tự hoá mà không đổi isolation của cả context và không làm `/unlock` hợp lệ của FE chậm hay đầy log deadlock. **Không** dùng `UPDATE … WHERE last_used_step < step` đơn lẻ (chỉ chặn replay mã đúng). Đây là **sửa luồng cũ** dùng bởi FE → cần test hồi quy. Test (T1): 20 request cùng mã → đúng 1 success; N mã sai song song → counter tăng đủ N và khoá đúng ngưỡng; test chéo `Unlock`/`Disable` và `Confirm` lặp không cấp token theo state cũ.

**Prompt injection / rò rỉ (rủi ro thật vì dùng chung chat với connector khác; Tung chấp nhận Q1 + Q5)**
- Thành thật: server **không ép được** model không làm theo chỉ thị trong nội dung task; biện pháp chỉ giảm nhẹ. Read-only/ghi-ít **không** ngăn việc Claude gửi dữ liệu đã đọc sang connector khác.
- Biện pháp thực (đã chốt Q7): `origin` bất biến do server đặt (`user|mcp`) + migration + DTO (**vì có `add_devlog`**) làm provenance; cắt độ dài lúc ghi (không strip URL — Q23); hạn mức ghi/ngày; kill switch. **Không** coi "luôn hỏi" là kiểm soát server-side (chỉ là cài đặt client). Hiển thị comment `origin=mcp` như trích dẫn không tin cậy ở FE/`superapp.py`: làm khi tiện (Q7).
- Hướng dẫn cho Tung: tránh bật connector gửi dữ liệu ra ngoài (email/web) trong cùng chat khi dùng connector SuperApp.

**Hạ tầng & vận hành**
- **Bước −1** (xem dưới). `UseForwardedHeaders` đúng (Cloudflare + nginx) và **đặt trước** redirect/limiter/auth; nginx **ghi đè** `CF-Connecting-IP`/`X-Forwarded-For`; backend chỉ tin proxy `127.0.0.1`; `ClientIp()` bỏ tin header client.
- Rate limit **riêng** cho OAuth/MCP: theo (IP tin cậy, endpoint, `client_id`), giới hạn đồng thời + body; sửa mô tả cũ ("10 req/s/IP cả backend" là sai: chỉ 3 endpoint login, `refresh` không có).
- **Cooldown ghi gắn tool + user (C21-11):** 1 lần/10 giây của `add_devlog` thi hành **trong `AddMcpDevlog`**, sau auth + validation header/body, khoá theo **user cố định + tool** (không theo IP/`client_id`) để đọc (`list_tasks`) không tiêu permit ghi và request vô danh/sai header từ cùng egress không khoá lần ghi hợp lệ; limiter IP/`client_id` ở edge là lớp chống abuse riêng (hiện limiter key theo IP và chạy trước auth — `Startup.cs:145-153,300-303`). Test: hai add đồng thời chỉ một permit.
- Cloudflare: rule bỏ challenge **có phạm vi** theo path/method đã đo cho `/mcp*`, `/mcp-oauth/*`, `/.well-known/*` (không bỏ rate limit); giữ proxy. Test **máy-máy** (fetch metadata, `401` của MCP không bị đổi thành HTML) — ghi status/`Content-Type`/first-byte; không có response HTML/challenge.
- nginx: mặc định phản hồi **JSON + deadline ngắn** (số cụ thể: deadline tool ≤ 10 s, body request `/mcp` ≤ 128 KiB (chừa overhead JSON/escape cho devlog 16 KiB UTF-8), **giới hạn response thực thi TRONG APP bằng **serialize cả trang một lần rồi kiểm byte JSON cuối; vượt cap thì giảm số item và serialize lại; nếu MỘT item đã vượt cap (mô tả 64 KiB × escape nở tới 6×) thì cắt mô tả của item đó dần cho tới khi vừa, đặt `truncated`; cursor luôn tiến** — KHÔNG dùng counting writer, KHÔNG làm ma trận control-char (B25 1(b)); test vài ca ASCII/Việt/emoji/`\u0000` (T1); nginx không có directive cap response tương đương nên không dựa vào nginx (C15-10)**, đồng thời ≤ 10); **chỉ bật SSE/streaming/subscription khi transcript client chứng minh cần** (không đòi "stream 5 phút"). `proxy_set_header Host $host`; trả `X-Accel-Buffering: no` khi mở SSE (spec khuyến nghị); GET/DELETE endpoint modern → 405; `limit_req`/`limit_conn`; TLS ≥ 1.2, HSTS. `allowed_hosts` = `www.tungle.uk`; Origin sai bị từ chối, vắng Origin cho phép.
- Audit log (ai, tool, task id, kết quả — **không** ghi tham số/nội dung/token); chốt thời hạn lưu.
- **Kill switch (đơn giản hoá theo B25 1(b)):** khẩn cấp = nginx `return 503` cho `/mcp*` và `/mcp-oauth/*` (`nginx -t && systemctl reload nginx`); thu hồi token = xoay khoá ký MCP (restart) hoặc xoá bảng refresh MCP; cờ `Mcp:Enabled=false` (đổi cấu hình + restart) tắt hẳn phần MCP. **Bỏ file-watcher fail-closed ≤2 giây** (cả ba cách đều cần SSH nên không đổi mức khẩn cấp). CHẤP NHẬN GIỚI HẠN: cờ cấu hình cần restart. Test (T1): bật 503 chặn authorize/refresh/tool; app thường vẫn sống.
- Xoay khoá ký (`kid`, chồng lấp), cảnh báo sắp hết hạn.

## Các bước

**Thứ tự (sửa theo B25 1(d)):** −1a (bắt buộc) → **0a ∥ Bước 1** (gia cố backend độc lập với spike) → 0b → 2/3 → 4 → 5; **−1b chạy song song, xong trước Bước 4**.

**Bước −1a — Đóng cổng DB (BẮT BUỘC, TRƯỚC mọi thứ; Q6) — theo thứ tự, health-check sau mỗi lớp**
0. **Preflight + inventory đầy đủ (B25-2):** sshd nghe `0.0.0.0:22` và `[::]:22`; **`ss -tulnp` mọi listener** (HTTP lẫn không-HTTP), `tailscale status`, **docker published ports** (docker tự thêm iptables nên **ufw KHÔNG chặn được cổng docker publish** — dùng chain `DOCKER-USER`), các dịch vụ đã biết trên VPS (`tung-root/context/services.md`: `tailscaled`, `openclaw-gateway.service` bind tailnet, `gemi2api-*` container, `mssql-server`, cron `/root/backup-superapp.sh`, tunnel dev `127.0.0.1:14330→1433`); liệt kê mọi client DB: backend production (**`DB_SERVER=157.66.101.51:1433` — nối qua IP công khai của VPS, nên bind SQL localhost lúc này sẽ làm app sập**), máy khác của Tung?; ghi lại healthcheck sẵn có (`tungroot-vps-healthcheck.sh`); đặt **rollback timer**; luôn giữ một phiên SSH thứ hai.
1. Đổi `DB_SERVER` của backend production sang `127.0.0.1`, restart, health-check app/DB/website.
2. Firewall nhà cung cấp VPS **deny 1433** từ ngoài (**Tung làm**) + ufw: **`allow in on tailscale0`**, `41641/udp` (Tailscale), 22/80/443 (v4+v6), **sau khi** xác nhận phiên SSH mới vẫn vào được. Sau `ufw enable`: chạy healthcheck + **gửi thử một hook Telegram/cron nhà↔VPS** (ufw mặc định `deny incoming` sẽ chặn Tailscale/openclaw/cron im lặng nếu quên rule).
3. **Sau cùng** mới cân nhắc bind SQL Server về localhost.
- Mỗi thay đổi cấu hình VPS **chỉ chạy sau khi Tung duyệt từng cái**. Xong khi: phiên SSH mới + app/DB/website + bot/hook pass sau từng lớp; quét từ hai mạng ngoài, 1433 timeout/filtered; rollback đã diễn tập.

**Bước −1b — Khoá origin (SONG SONG, xong TRƯỚC Bước 4; KHÔNG chặn việc code MCP — Q25)**
- Inventory `nginx -T`, hostname/proxy/ACME, site DNS-only; `default_server` **thay bằng catch-all từ chối**; `UseForwardedHeaders` đúng (xem Bước 1); nginx ghi đè header IP; backend chỉ tin proxy `127.0.0.1`.
- **AOP per-hostname (theo tài liệu Cloudflare — A đã đối chiếu):** tự tạo CA riêng; **leaf cert + private key** (upload root CA thất bại) tải lên Cloudflare gắn hostname `www`; cài CA ở origin; nginx `ssl_client_certificate` + `ssl_verify_client optional` **chỉ trong server block `www`**, log `$ssl_client_verify`/serial/subject, chỉ đổi `on` khi MỌI request qua Cloudflare ghi `SUCCESS` (`optional` vẫn trả 200 khi không có cert); default 443 `ssl_reject_handshake on` (kiểm version); rollback timer, cảnh báo hết hạn, khoá riêng trong vault. Nếu dùng ACL IP thì gate trên `$realip_remote_addr`, **không** `$remote_addr`. Rủi ro nó chặn: kẻ biết IP gốc né WAF/rate-limit Cloudflare (không né auth) → là lớp phụ, nên không phải tiên quyết (B25).
- Test: qua Cloudflare được 200/401; `curl --resolve` thẳng origin + header `CF-Connecting-IP` giả thất bại; vhost DNS-only và ACME pass; no-SNI/SNI-Host mismatch/cert sai/hết hạn fail.

**Bước 0a — Đo transport/client (bearer tĩnh, dữ liệu giả; KHÔNG đo OAuth)**
- Kiểm iOS tối thiểu của app Claude; custom connector có trên app iOS / Safari / claude.ai / Claude Code không. MCP giả trên subdomain tạm **có bearer test ngắn hạn hoặc Cloudflare Access + TTL tự huỷ** (không dựa vào URL khó đoán); gỡ hết sau khi xong.
- Ghi **transcript**: protocol version thật từng client, loại response (JSON/SSE), Cloudflare có đổi `401` thành HTML không (kiểm cả `200` metadata lẫn `401` MCP). **Không suy OAuth từ bearer** — bearer không sinh request authorize/token/refresh (C8-6).
- **Spec là việc của SDK + thư viện OAuth, plan không chép lại (B25 1(b)).** Contract test 0a: SDK/thư viện phiên bản đã pin pass bộ kiểm tra của **hai client thật**: `server/discover` (server MUST — client MAY gọi; test cả client gọi lẫn client gọi inline), ba header `MCP-Protocol-Version`/`Mcp-Method`/`Mcp-Name` khớp body (sai/thiếu → `400` + `-32020 HeaderMismatch`), phiên bản không hỗ trợ → `UnsupportedProtocolVersionError`, GET/DELETE → `405`, bỏ qua `Mcp-Session-Id`/`Last-Event-ID`, SSE kèm `X-Accel-Buffering: no`. **Ghi era thật từng client** (không suy "SDK v2 = client modern"); legacy `initialize` chỉ bật khi client thật cần; proxy/Cloudflare không làm rơi `Mcp-Name`.
- **Quyết định đi/không đi 0b:** nếu iPhone không dùng được connector thì plan chuyển thành "chỉ Claude Code + Safari/claude.ai" **trước khi viết AS**.

**Bước 0b — OAuth thử nghiệm: AS bằng thư viện thật + danh tính GIẢ (chỉ khi 0a cho phép đi tiếp)**
- Đo OAuth: cơ chế đăng ký client (CIMD / pre-registration / DCR; client nhập `client_id` tay không), redirect URI thực, `resource` ở authorize/token/**refresh** (có gửi lại không), refresh đồng thời, PRM/metadata đúng URL, `iss` trong response. Mỗi cột ma trận ghi **artefact request nào sinh ra nó**.
- **Vòng đời token trên thiết bị thật (B25-5, Q27):** đo hai ca trên MỖI client (iPhone, Claude Code): (a) **refresh sau khi app ngủ 3 ngày** — refresh có êm không; (b) **hết hạn tuyệt đối** — client tự đăng nhập lại hay hiện lỗi mơ hồ. Ghi hành vi UI từng client; chốt số ngày tuyệt đối (đề nghị 90, Q27) theo transcript.
- Kiểm 4 điều kiện của Q3 trên thư viện (end-to-end bằng client thật); **pin version MCP C# SDK v2.x và thư viện OAuth trong artefact spike (transcript + package lock), không pin bằng chữ trong plan** (C15-1). Không đạt → dừng, đổi giải pháp (Q9). **Không có** nhánh tự viết và **không có** token tĩnh dài hạn dự phòng. **Sau cùng** mới nối Google/TOTP thật (Bước 2). **Không tạo migration/schema OAuth production và không nối Google thật trước khi 0b có transcript** (C21-1).

**Bước 1 — Gia cố backend**
- **Cờ tách deploy khỏi mở tính năng:** `Mcp:Enabled=false` mặc định; mọi code MCP/OAuth ship ở trạng thái tắt (B25-1, B25-3).
- Bật kiểm `iss`/`aud` cho luồng hiện có **sau cờ cấu hình** (thử kỹ FE + `superapp.py`); `UseForwardedHeaders`; đăng ký `SecurityHeadersMiddleware`; khoá ký bất đối xứng + JWKS; `Mcp:Enabled` + nginx 503 (kill switch đã đơn giản hoá); endpoint `/api/diagnostic/logs` ẩn danh: xem lại.
- **Forwarded proto (C15-6):** nginx ghim `X-Forwarded-Proto https`; ASP.NET chỉ tin proxy `127.0.0.1` và chạy `UseForwardedHeaders` **trước** `UseHttpsRedirection` (hiện `Startup.cs:284-` redirect HTTPS trước routing/auth) để không loop và không sinh URL `http://`; 0b lưu `Location`/issuer tuyệt đối. Test: header giả từ internet không đổi scheme/client IP.

**Bước 2 — Authorization server (cô lập) + `IMcpFacade` + `origin`**
- OAuth theo Q3 + TOTP nội bộ + consent; `IMcpFacade`; migration `origin` + DTO + hạn mức ghi.
- Test ca xấu: `sub`/email lạ, `email_verified=false`, redirect lạ, PKCE sai/thiếu, `resource` sai, login CSRF (bắt đầu ở trình duyệt khác), refresh dùng lại, redeem code hai lần song song, consent bị nhúng iframe, đăng ký client dồn dập (DCR tắt).

**Bước 3 — MCP server (C# SDK) trong process**
- 4 tool v1; scheme `Mcp`; scope kiểm trong từng tool; rate limit; Origin/Host; giới hạn kích thước; audit; làm sạch output.
- Test bằng client MCP thật + ca xấu: không token, sai scope/`aud`/`iss`/`typ`/`alg`, hết hạn, `sub` khác, token MCP gọi API thường (phải 401) và ngược lại, input lớn, id lạ, vượt rate limit, Origin lạ, nội dung task chứa chỉ thị (không tự kích hoạt gì), `add_devlog` không forge `origin`.

**Bước 4 — Lên VPS**: cấu hình nginx (route `/mcp`, `/mcp-oauth`, `/.well-known/*`, streaming, giới hạn), Cloudflare rule, secret qua vault; kiểm không lộ cổng nội bộ, kill switch (cả nginx 503).

**Bước 5 — Nối vào Claude, thử thật**: iPhone + Claude Code: đọc task, ghi devlog vào task nháp, bị từ chối khi sai quyền, thử kill switch.

**Bước 6 — Mở rộng**: thêm tool theo từng đợt có test (tick checklist, comment…); mỗi lần thêm tool **ghi** thì xem lại Q2 (cách ly process) và threat model. Finance chỉ khi thật cần, scope riêng, mặc định tắt.

## Deploy, rollback, migration (B25-3)

- Backend là `superapp-api.service` (systemd), **không có staging**: publish vào thư mục có version, symlink `current`, **rollback = đổi symlink + restart** (diễn tập một lần, < 2 phút).
- Migration schema (`migrations/` là SQL tay, repo không dùng EF migrations): **chỉ thêm** (cột `origin` default `'user'`, bảng refresh MCP, bộ đếm quota, audit), chạy **sau backup 6h**, mỗi file có `*_rollback.sql`.
- Bật kiểm `iss`/`aud` cho luồng hiện có: token hiện tại **có** mang `iss`/`aud` (`AuthService.cs:480-486`, `Jwt:Issuer` mặc định `SuperApp`, `Jwt:Audience` `SuperApp-API`) nên rủi ro thấp hơn lo ngại ban đầu, nhưng vẫn đặt sau cờ `Jwt:ValidateIssuerAudience` **mặc định false ở release đầu**, bật ở release sau khi log xác nhận 100% token khớp; thử FE + `superapp.py` sau mỗi bước.

## Test theo bậc (B25-4, Q26) — mọi "Test:" ở trên thuộc T1 trừ khi ghi T2

- **T1 — bắt buộc trước Bước 5:** hai user thật (list/get/add không chéo); 20 TOTP song song → 1 success, N mã sai → counter N; 50 `add_devlog` song song đếm đúng quota; token MCP ↔ API thường chéo → 401 cả hai chiều; cap/JSON cuối vài ca; nginx 503 chặn authorize/refresh/tool; `SuperAppUserId` sai → MCP tắt, app sống; hồi quy FE `/unlock`. Harness: một project xunit trên SQL Server LocalDB/docker + `Respawn` (ước ~1 ngày — **ước lượng của B, A chưa xác minh**).
- **T2 — trước khi mở tool ghi thứ hai:** fault injection tại `SaveChanges`/`Commit`, kill process 100 lần, working set với trường rất lớn, rotate cert AOP.
- **T3 — BỎ:** ma trận 20 MiB/trường, control-char matrix, diễn tập chồng lấp rotate nhiều lần.

## Việc Tung cần kiểm / làm

1. **Firewall nhà cung cấp VPS: deny cổng 1433** và kiểm từ mạng khác (Bước −1a, mục 2).
2. Cài đặt Cloudflare cho `www.tungle.uk` (Bot Fight/WAF/timeout); app Claude iOS có "thêm custom connector" không.
3. **Duyệt từng thay đổi VPS ở Bước −1a/−1b** (đặc biệt: app đang nối DB qua IP công khai nên phải đổi cấu hình app trước khi đóng cổng DB).

## Làm xong khi

Từ iPhone (máy ở nhà tắt), nói "ghi devlog task N …" và nó ghi đúng, đọc được task/plan; mọi ca xấu bị từ chối; có audit log; không secret nào trong repo; kill switch đã thử (kể cả nginx 503); token MCP không dùng được trên API thường; cổng DB và origin đã đóng.
