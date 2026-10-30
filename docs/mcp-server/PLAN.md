# MCP server cho SuperApp — plan v5

> Task SuperApp #1734. Bản canonical của plan nằm ở file này (trong repo); mô tả task trên SuperApp là bản sao. Sàn tranh luận: `debates/2026-10-30-mcp-server.md` (A = Claude, C = GPT/Codex, Tung quyết). v5 = v4 + các quyết định của Tung ở Vòng 4 + phản biện của C ở Vòng 2.

## Mục tiêu

Dùng SuperApp (task, devlog, plan) từ Claude trên **iPhone (iOS 18.5)** và Claude Code mà không cần máy ở nhà bật hay session remote: một **MCP server** thêm vào tài khoản Claude như custom connector. Bảo mật tối đa ngay từ đầu.

## Quyết định đã chốt (Tung, 30/10)

| # | Quyết định | Ghi chú |
|---|---|---|
| Q1 | **Bản đầu: xem + ghi devlog** (`add_devlog` chỉ tạo mới). Không có `tick_checklist`, không xoá, không sửa. | A và C đều đề nghị chỉ-đọc; Tung chọn thêm devlog → rủi ro prompt injection đường ghi **được chấp nhận có điều kiện** (xem mục Injection). |
| Q2 | **MCP chạy cùng process ASP.NET** (MCP C# SDK), **không** service key / unix socket / tiến trình Python riêng. Tool handler đi qua `IMcpFacade` hẹp, không gọi service layer trực tiếp. | Chấp nhận giới hạn: MCP bị chiếm (RCE) = chiếm cả app. Xem lại nếu mở thêm tool ghi. |
| Q3 | **Authorization server (OAuth) trong backend, dùng thư viện OAuth cho .NET** (ứng viên OpenIddict) — **chỉ nếu spike đạt 4 điều kiện**: hỗ trợ `resource` (RFC 8707); consent tuỳ biến; đăng ký sẵn 2 client, tắt DCR; bảng token tách riêng. Nếu không đạt: AS tối giản tự viết **kèm** test redeem/refresh song song. | Mặc định theo đề nghị (Tung không trả lời riêng câu này). |
| Q4 | **Đòi TOTP** khi cấp quyền (sau khi Google `sub` khớp) — gọi **service verify nội bộ**, giữ replay/lockout; không gỡ `[Authorize]` của `TotpController`. Recovery chỉ qua SSH/runbook. | |
| Q5 | **Task nhạy cảm (`is_sensitive`) ĐƯỢC đọc qua MCP.** | Tung chọn khác đề nghị của A. **Chấp nhận giới hạn**: Claude có thể đọc rồi (qua connector khác trong cùng chat) gửi ra ngoài. Giảm nhẹ: finance và project nội bộ vẫn **ngoài phạm vi**; kill switch; hướng dẫn không bật connector gửi dữ liệu ra ngoài khi dùng SuperApp. |
| Q6 | **Bước −1 (đóng cửa hạ tầng) làm TRƯỚC mọi thứ.** | Phần chặn ở bảng điều khiển nhà cung cấp VPS: Tung tự làm. |

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
   /mcp-oauth/*   authorization server (thư viện OAuth hoặc tối giản tự viết — theo spike)
   │
   ▼ `IMcpFacade` (hẹp): ListTasks, GetTask, GetPlan, AddDevlog — trả DTO tối giản, KHÔNG phải service layer
   ▼
Database
```

- Scheme `Mcp` **ngoài chính sách mặc định**; token MCP không dùng được trên API thường và ngược lại (test cả hai chiều).
- **Không** tạo `McpController` nội bộ song song với facade (chọn một đường: facade).
- Khoá ký **bất đối xứng riêng** cho token MCP (khác `Jwt:Key`); `kid`; xoay được.

## Công cụ v1

`list_tasks`, `get_task`, `get_plan` (đọc); `add_devlog` (ghi, **chỉ tạo mới**, không nhận id, cắt độ dài, bỏ URL ngay lúc ghi, hạn mức/ngày ở backend, gắn `origin = mcp`).
Không có: xoá, sửa task, tick checklist, comment khác, project, finance, SQL, gọi API tuỳ ý.

## Bảo mật

**OAuth / xác thực**
- S256 only; `resource` bắt buộc ở authorize; ở **token/refresh** bắt buộc **khi spike xác nhận client gửi** (không mở "tương thích" bằng cách chấp nhận thiếu `resource` nếu client đúng chuẩn gửi). Một `canonical_resource` duy nhất: PRM `resource`, `aud`, `iss` khớp **từng ký tự** (host `www.tungle.uk`).
- Metadata: `/.well-known/oauth-authorization-server` (kèm `code_challenge_methods_supported:["S256"]`; chỉ quảng bá `authorization_response_iss_parameter_supported:true` nếu thật sự trả `iss`), `/.well-known/oauth-protected-resource` (đường dẫn có hậu tố theo RFC 9728/8414); nginx route đúng; MCP trả `401 WWW-Authenticate` đúng chuẩn. **Các điều khoản/revision spec chưa được kiểm → contract test ở Bước 0**, không coi là sự thật.
- Redirect URI: allowlist `https://claude.ai/api/mcp/auth_callback` (+ claude.com) và loopback mọi cổng (RFC 8252) cho Claude Code; không redirect trước khi validate `client_id` + `redirect_uri`. **DCR tắt**, 2 client đăng ký sẵn.
- Chống login CSRF/mix-up: cookie `__Host-` SameSite=Lax gắn yêu cầu authorize, bắt buộc ở callback Google + POST consent; token CSRF; `frame-ancestors 'none'` + `X-Frame-Options` (đăng ký `SecurityHeadersMiddleware` — hiện có file nhưng chưa đăng ký); luôn hiện consent.
- Đường authorize MCP riêng: chỉ xin `openid email`; **không đụng** `GoogleRefreshToken`/Drive (`AuthService.cs:153`); allowlist `sub` **trước** mọi tra cứu/tạo user; không trả lỗi Google nguyên văn.
- Refresh MCP ở **bảng riêng**, mỗi grant một gia đình, gắn `client_id` + `resource`, xoay mỗi lần dùng, dùng lại = thu hồi **cả gia đình** (không dùng `RefreshTokenAsync` hiện có), hạn tuyệt đối 30 ngày. Redeem code / refresh phải **nguyên tử** (cùng transaction) + test song song.
- Access token ≤ 15'; ký sai / `aud` / `iss` / `typ` / `alg` sai (kể cả `none`, HS256) bị từ chối.
- TOTP: AS gọi service verify nội bộ sau khi `sub` khớp; test không JWT vẫn hoàn tất flow, sai/replay bị khoá, `sub` lạ không tới bước TOTP.

**Prompt injection / rò rỉ (rủi ro thật vì dùng chung chat với connector khác; Tung chấp nhận Q1 + Q5)**
- Thành thật: server **không ép được** model không làm theo chỉ thị trong nội dung task; biện pháp chỉ giảm nhẹ. Read-only/ghi-ít **không** ngăn việc Claude gửi dữ liệu đã đọc sang connector khác.
- Biện pháp thực: `origin` bất biến do server đặt (`user|mcp`) + migration + DTO (**vì có `add_devlog`**); CLI (`superapp.py`) và FE hiển thị comment `origin=mcp` như văn bản trích dẫn không tin cậy; cắt độ dài + bỏ URL lúc ghi; output đọc trả trường tối giản; tool ghi đặt "luôn hỏi"; hạn mức ghi/ngày; **kill switch**.
- Hướng dẫn cho Tung: tránh bật connector gửi dữ liệu ra ngoài (email/web) trong cùng chat khi dùng connector SuperApp.

**Hạ tầng & vận hành**
- **Bước −1** (xem dưới). `UseForwardedHeaders` đúng (Cloudflare + nginx) và **đặt trước** redirect/limiter/auth; nginx **ghi đè** `CF-Connecting-IP`/`X-Forwarded-For`; backend chỉ tin proxy `127.0.0.1`; `ClientIp()` bỏ tin header client.
- Rate limit **riêng** cho OAuth/MCP: theo (IP tin cậy, endpoint, `client_id`), giới hạn đồng thời + body; sửa mô tả cũ ("10 req/s/IP cả backend" là sai: chỉ 3 endpoint login, `refresh` không có).
- Cloudflare: rule bỏ challenge **có phạm vi** cho `/mcp*`, `/mcp-oauth/*`, `/.well-known/*` (không bỏ rate limit); test máy-máy (fetch metadata không phải trình duyệt), ghi status/`Content-Type`/first-byte/completion; không có response HTML/challenge.
- nginx streaming: `proxy_buffering off`, `proxy_read_timeout` dài, `proxy_set_header Host $host`; `limit_req`/`limit_conn`; body nhỏ cho `/mcp`; TLS ≥ 1.2, HSTS. `allowed_hosts` = `www.tungle.uk`; Origin sai bị từ chối, vắng Origin cho phép.
- Audit log (ai, tool, task id, kết quả — **không** ghi tham số/nội dung/token); chốt thời hạn lưu.
- **Kill switch**: cờ trong **DB/file theo dõi** (không cần restart) kiểm ở mọi request OAuth/MCP + lệnh thu hồi mọi gia đình refresh; nhanh nhất: nginx `return 503` cho `/mcp*` + tắt route; có test.
- Xoay khoá ký (`kid`, chồng lấp), cảnh báo sắp hết hạn.

## Các bước

**Bước −1 — Đóng cửa hạ tầng (TRƯỚC mọi thứ; đã chốt Q6)**
1. Firewall nhà cung cấp VPS **deny 1433** từ ngoài (**Tung làm**); bind SQL Server về localhost; bật ufw chỉ 22/80/443.
2. Origin chỉ nhận **dải Cloudflare** ở 80/443 (hoặc Authenticated Origin Pulls); bỏ `default_server` phục vụ app theo IP.
3. nginx ghi đè header IP; backend chỉ tin proxy `127.0.0.1`.
- Mỗi thay đổi cấu hình VPS **chỉ chạy sau khi Tung duyệt từng cái**. Xong khi: quét từ hai mạng ngoài, 1433 timeout/filtered; gọi thẳng IP:80/443 thất bại; qua Cloudflare với header giả vẫn rate-limit theo IP thật; app vẫn kết nối DB.

**Bước 0 — Spike NHẸ (không xác thực, dữ liệu giả)**
- Kiểm iOS tối thiểu của app Claude; custom connector có trên app iOS/Safari/claude.ai không; MCP giả không xác thực, subdomain tạm **có bearer test ngắn hạn hoặc Cloudflare Access + TTL tự huỷ** (không dựa vào URL khó đoán); thử OAuth tách riêng với khoá test; gỡ hết sau khi xong.
- Ghi: revision MCP/OAuth thật client dùng; DCR/CIMD/nhập `client_id` tay; redirect URI thực; có gửi `resource` khi refresh không; refresh đồng thời; PRM/metadata đúng URL; Cloudflare chặn/cắt stream không (stream 5 phút, test máy-máy).
- Kiểm 4 điều kiện của Q3 cho thư viện OAuth. **Không có** phương án dự phòng token tĩnh dài hạn.

**Bước 1 — Gia cố backend**
- Bật kiểm `iss`/`aud` cho luồng hiện có **sau cờ cấu hình** (thử kỹ FE + `superapp.py`); `UseForwardedHeaders`; đăng ký `SecurityHeadersMiddleware`; khoá ký bất đối xứng + JWKS; cờ kill switch; endpoint `/api/diagnostic/logs` ẩn danh: xem lại.

**Bước 2 — Authorization server (cô lập) + `IMcpFacade` + `origin`**
- OAuth theo Q3 + TOTP nội bộ + consent; `IMcpFacade`; migration `origin` + DTO + hạn mức ghi.
- Test ca xấu: `sub`/email lạ, `email_verified=false`, redirect lạ, PKCE sai/thiếu, `resource` sai, login CSRF (bắt đầu ở trình duyệt khác), refresh dùng lại, redeem code hai lần song song, consent bị nhúng iframe, đăng ký client dồn dập (DCR tắt).

**Bước 3 — MCP server (C# SDK) trong process**
- 4 tool v1; scheme `Mcp`; scope kiểm trong từng tool; rate limit; Origin/Host; giới hạn kích thước; audit; làm sạch output.
- Test bằng client MCP thật + ca xấu: không token, sai scope/`aud`/`iss`/`typ`/`alg`, hết hạn, `sub` khác, token MCP gọi API thường (phải 401) và ngược lại, input lớn, id lạ, vượt rate limit, Origin lạ, nội dung task chứa chỉ thị (không tự kích hoạt gì), `add_devlog` không forge `origin`.

**Bước 4 — Lên VPS**: cấu hình nginx (route `/mcp`, `/mcp-oauth`, `/.well-known/*`, streaming, giới hạn), Cloudflare rule, secret qua vault; kiểm không lộ cổng nội bộ, kill switch (cả nginx 503).

**Bước 5 — Nối vào Claude, thử thật**: iPhone + Claude Code: đọc task, ghi devlog vào task nháp, bị từ chối khi sai quyền, thử kill switch.

**Bước 6 — Mở rộng**: thêm tool theo từng đợt có test (tick checklist, comment…); mỗi lần thêm tool **ghi** thì xem lại Q2 (cách ly process) và threat model. Finance chỉ khi thật cần, scope riêng, mặc định tắt.

## Việc Tung cần kiểm / làm

1. **Firewall nhà cung cấp VPS: deny cổng 1433** và kiểm từ mạng khác (Bước −1.1).
2. Cài đặt Cloudflare cho `www.tungle.uk` (Bot Fight/WAF/timeout); app Claude iOS có "thêm custom connector" không.

## Làm xong khi

Từ iPhone (máy ở nhà tắt), nói "ghi devlog task N …" và nó ghi đúng, đọc được task/plan; mọi ca xấu bị từ chối; có audit log; không secret nào trong repo; kill switch đã thử (kể cả nginx 503); token MCP không dùng được trên API thường; cổng DB và origin đã đóng.
