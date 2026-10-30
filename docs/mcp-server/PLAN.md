# MCP server cho SuperApp — plan v4

> Task SuperApp #1734. Bản canonical của plan nằm ở file này (trong repo); mô tả task trên SuperApp là bản sao. Sàn tranh luận: `debates/2026-10-30-mcp-server.md`.

## Mục tiêu

Dùng SuperApp (task, devlog, checklist, plan…) từ Claude trên **iPhone (iOS 18.5)** và Claude Code mà không cần máy ở nhà bật hay session remote: một **MCP server** trên VPS, thêm vào tài khoản Claude như custom connector. Bảo mật tối đa ngay từ đầu. Plan v4 = v3 + sửa theo review độc lập lần 2 + dữ kiện đã kiểm trên VPS.

## Dữ kiện đã kiểm (30/10)

- **Host chính thức: `www.tungle.uk`** (`tungle.uk` chuyển hướng 301 sang `www`). Mọi URL OAuth/MCP, `iss`, `aud`, metadata dùng host này, khớp từng ký tự.
- Domain đứng sau **Cloudflare** (proxy bật; nginx có `real_ip_header CF-Connecting-IP` + dải IP Cloudflare). → IP thật lấy từ `CF-Connecting-IP`, chỉ tin khi request đến từ dải Cloudflare; **Cloudflare có thể chặn/giới hạn kết nối từ máy chủ Anthropic hoặc cắt stream dài** → kiểm ở Bước 0.
- **Kestrel chỉ nghe `127.0.0.1:5000`** (và `[::1]`), `ASPNETCORE_URLS=http://localhost:5000`, chạy bằng user `www-data`. nginx proxy `/api` → `127.0.0.1:5000`; `www.tungle.uk` 443 (Certbot), `tungle.uk` 80 có redirect HTTPS.
- **`ufw` đang tắt, iptables INPUT ACCEPT**; **SQL Server nghe `0.0.0.0:1433`**. Ghi chú cũ nói "cổng 1433 public đã chặn" (có thể chặn ở firewall của nhà cung cấp VPS) — **chưa xác minh được từ ngoài** (mạng máy hiện tại cho kết quả giả) → Tung kiểm lại (xem mục "Việc Tung cần kiểm").
- Có nhiều site khác trên cùng nginx (glancify-dev, pets, portfolio) — không đụng.
- iPhone: **iOS 18.5** (iPhone XS/11 Pro Max trở lên) — không bị giới hạn iOS 16 như giả định ban đầu. Chưa biết app Claude trên iOS có hỗ trợ custom connector/OAuth đầy đủ không → Bước 0.

## Quyết định đã chốt

- **Chỉ một danh tính** dùng MCP, ghim bằng **Google `sub`** (không chỉ email): bắt `email_verified`, chuẩn hoá chữ hoa/thường; `sub` khớp được kiểm ở **cả MCP server lẫn backend**. SuperApp vẫn cho đăng ký tự do (product); luồng MCP không tạo user. Siết đăng ký tự do: chỉ làm khi tiện.
- **Authorization Server đặt trong backend, cô lập** (route/bảng/khoá ký/Google client riêng); nếu rủi ro thì tách tiến trình nhỏ riêng sau.
- **Rủi ro dùng chung chat với connector khác (Gmail, web…) là thật** → xử lý prompt injection nghiêm túc.
- **Đăng ký client động (DCR) tắt**; đăng ký sẵn 2 client (claude.ai, Claude Code). Chỉ bật CIMD nếu client bắt buộc. Kiểm ở Bước 0 xem Claude cho nhập client_id thủ công không.

## Kiến trúc

```
Claude (iPhone / Claude Code)
   │ (1) OAuth 2.1 + PKCE(S256) qua BACKEND (đăng nhập Google, đồng ý scope)
   │     → ACCESS TOKEN JWT ES256 (aud = https://www.tungle.uk/mcp, sub = Google sub, ≤15') + REFRESH (bảng riêng)
   ▼
Cloudflare → nginx (HTTPS) → MCP server (127.0.0.1)  ← resource server, CHỈ KIỂM token (khoá CÔNG KHAI nạp từ đĩa)
   │ (2) kiểm chữ ký + typ + iss + aud + exp + sub + scope TRONG TỪNG TOOL → rate limit → audit
   │ (3) gọi backend qua UNIX SOCKET / listener riêng, bằng SERVICE KEY (gắn cứng 1 user)
   ▼
Backend: McpController riêng (endpoint dựng riêng, KHÔNG dùng lại controller thường) → DB
```

- **Token A không bao giờ đi tiếp sang backend.**
- **Service key**: gắn cứng một userId + `sub` lúc cấp; scheme xác thực `McpService` **nằm ngoài chính sách mặc định**; middleware **deny-by-default** (403 mọi endpoint không đánh dấu `[McpAllowed]`); chỉ chấp nhận khi request đến từ **listener riêng** (unix socket quyền 0600 hoặc cổng 127.0.0.1 nginx không proxy), listener này **miễn chuyển hướng HTTPS**; lưu hash; thu hồi/xoay được.
- **McpController** (endpoint dựng riêng, không phải PATCH/DELETE tổng quát):
  - `add_devlog`: chỉ tạo mới, không nhận id.
  - `tick_checklist`: tick theo id mục ngay trên server (không ghi đè cả `checklistJson`, tránh mất dữ liệu khi sửa đồng thời từ web).
  - Đọc: DTO chỉ trả trường cần thiết.
  - **Hạn mức ghi theo ngày đặt ở backend**; mọi thay đổi gắn nhãn nguồn "qua MCP".

## Nguyên tắc bảo mật

**OAuth / xác thực**
- S256 only; bắt `resource` ở authorize + token (kiểm Claude có gửi khi refresh không trước khi ép); so URL chuẩn hoá; `aud` lấy từ đó. Access token ≤ 15'.
- Metadata: `/.well-known/oauth-authorization-server` (có `code_challenge_methods_supported:["S256"]`, `iss` khớp host `www`), `/.well-known/oauth-protected-resource`; **nginx phải route đúng các đường `/.well-known/*` (kể cả dạng có hậu tố path RFC 8414/9728)**, nếu không frontend trả HTML 200 làm hỏng discovery. MCP trả `401 WWW-Authenticate: Bearer resource_metadata=…`.
- **Verifier tự viết** (SDK `mcp` 2.x chỉ có interface `TokenVerifier`): pyjwt, `algorithms=["ES256"]`, bắt `typ=at+jwt`, `exp`, `iss`, `aud`, `sub`; JWKS nạp từ đĩa (không gọi mạng); điền `AccessToken.subject`/`resource`; bật `validate_token_resource=True`; **kiểm scope trong từng tool** (SDK chỉ kiểm theo cả endpoint).
- **Redirect URI**: allowlist `https://claude.ai/api/mcp/auth_callback` (+ claude.com) và loopback `127.0.0.1`/`localhost` mọi cổng cho Claude Code; không redirect trước khi validate `client_id` + `redirect_uri`.
- **Chống login CSRF / mix-up**: cookie `__Host-` (SameSite=Lax) gắn với yêu cầu authorize, bắt buộc ở callback Google và ở POST đồng ý; token CSRF trong form đồng ý; `frame-ancestors 'none'` + `X-Frame-Options` (`SecurityHeadersMiddleware` có sẵn nhưng chưa đăng ký ở `Startup.Configure` → đăng ký); trả `iss` trong phản hồi (RFC 9207); **luôn hiện màn hình đồng ý** (tên client, host redirect, scope).
- **Đường authorize riêng cho MCP**: chỉ xin `openid email`; **không đụng** `GoogleRefreshToken`/Drive (AuthService.cs:153 hiện ghi đè); kiểm allowlist `sub` **trước** mọi tra cứu/tạo user; không trả nguyên văn lỗi của Google ra ngoài (tránh mẫu `Error = ex.Message` ở AuthService.cs:130,380).
- **Refresh MCP ở bảng riêng**, mỗi grant một gia đình, gắn `client_id` + `resource`, xoay mỗi lần dùng, **nghiêm ngặt: dùng lại = thu hồi cả gia đình** (không ân hạn; không dùng lại `RefreshTokenAsync` hiện có — nó có ân hạn 30s và thu hồi theo thiết bị/toàn bộ); hạn tuyệt đối 30 ngày; đo ở Bước 0 xem Claude có refresh đồng thời không.
- State/nonce/PKCE lưu **phía server, dùng một lần ~10'**; **Google OAuth client + redirect URI riêng** cho MCP.

**Quyền & công cụ**
- Scope hẹp (`tasks:read`, `tasks:write`, `plan:read`…); annotation `readOnlyHint`/`destructiveHint` đúng; **tool ghi đặt "luôn hỏi" trong Claude**.
- Không có tool xoá / đổi cài đặt / SQL / gọi API tuỳ ý; **finance ngoài phạm vi**; không nối thẳng DB.
- Kiểm PATCH task có giữ lịch sử không; nếu không, lưu **snapshot trước/sau** trong audit log cho thay đổi ghi.

**Prompt injection / rò rỉ (dùng chung chat với connector khác)**
- Thành thật: "bọc output là dữ liệu" và "tool không tự dùng output tool khác" **không do server ép được** — chỉ là giảm nhẹ. Biện pháp thực: **ghi vào backend với nhãn nguồn "qua MCP"**; CLI (`tungroot.py`) và FE **hiển thị comment nguồn MCP như văn bản trích dẫn không tin cậy**; cắt độ dài + bỏ URL **ngay lúc ghi**; bỏ trường thừa khi đọc; tool ghi "luôn hỏi"; hạn mức ghi/ngày. Lý do: nội dung ghi hôm nay có thể bị phiên Claude Code có shell đọc lại sau này.
- Hướng dẫn cho Tung: tránh bật connector có thể gửi dữ liệu ra ngoài (email/web) trong cùng chat khi dùng connector SuperApp có quyền ghi.

**Hạ tầng**
- Cloudflare: IP thật qua `CF-Connecting-IP` (chỉ tin từ dải Cloudflare); `UseForwardedHeaders` đúng `KnownProxies`/`ForwardLimit`; đảm bảo Anthropic không bị Bot/WAF chặn (rule cho `/mcp*`, `/.well-known/*`, `/mcp-oauth/*`), kiểm timeout/stream dài.
- nginx cho streamable HTTP: `proxy_buffering off`, `proxy_read_timeout` dài, **`proxy_set_header Host $host`** (nếu không `allowed_hosts` kiểm sai host); `limit_req` cho authorize/token; `limit_conn` cho `/mcp`; `client_max_body_size` nhỏ cho `/mcp`; TLS ≥ 1.2, HSTS.
- Rate limit backend hiện 10 req/s/IP (Startup.cs:150) quá lỏng cho OAuth → giới hạn riêng chặt hơn.
- Kiểm Origin/Host: từ chối Origin sai, **cho phép Origin vắng**; `allowed_hosts` = `www.tungle.uk`.
- systemd: user riêng, `NoNewPrivileges`, `ProtectSystem=strict`, secret qua `LoadCredential`; không dùng argument.
- Audit log: ai, tool, task id, kết quả — **không** ghi tham số/nội dung/token; chốt thời hạn lưu.
- **Công tắc tắt khẩn cấp**: cờ lưu trong **DB hoặc file theo dõi** (không cần restart) kiểm ở mọi request OAuth/service key + lệnh thu hồi mọi gia đình refresh + thu hồi service key; **cách nhanh nhất ghi sẵn: nginx `return 503` cho `/mcp*` + `systemctl stop`**; có test.
- Xoay khoá: `kid`, chồng lấp khi xoay service key, cảnh báo sắp hết hạn.
- Ghi chú vault: repo tung-vault đang ở sai account (xem CLAUDE.md) — không chặn việc này.

## Các bước

**Bước 0 — Bản thử NHẸ (spike) để biết điện thoại có chạy được không**
- Trước hết kiểm: iOS tối thiểu của app Claude; custom connector có trên app iOS và Safari/claude.ai không.
- Dựng **MCP server giả, không xác thực, dữ liệu giả**, ở subdomain tạm — chỉ để xem connector có hiện/gọi được trên iPhone, Safari, Claude Code; đo Cloudflare có chặn/cắt stream không.
- Thử OAuth **tách riêng**, không dùng DB thật hay Google client thật; xong thì **gỡ hết**.
- Ghi: DCR/CIMD/nhập client_id tay, redirect URI thực tế, có gửi `resource` khi refresh không, có refresh đồng thời không.
- **Bỏ hẳn phương án dự phòng token tĩnh dài hạn.**

**Bước 1 — Gia cố backend (trước mọi thứ của MCP)**
- Bật kiểm `iss`/`aud` cho luồng hiện có **sau cờ cấu hình** (thử kỹ FE + `superapp.py`, có đường lui). Lưu ý: đây là vệ sinh, không phải cách ly — token MCP bị từ chối chủ yếu vì khác thuật toán/khoá.
- `UseForwardedHeaders` đúng (Cloudflare + nginx); đăng ký `SecurityHeadersMiddleware`; cân nhắc `ClientIp()` dùng `RemoteIpAddress` thay vì `X-Forwarded-For` thô (AuthController.cs:49-55); endpoint `/api/diagnostic/logs` ẩn danh → xem lại.
- Khoá ký bất đối xứng + JWKS; cờ tắt khẩn cấp (DB/file); listener riêng cho service key.

**Bước 2 — Backend: service key + `McpController` + authorization server (cô lập)**
- `McpController`, scheme `McpService`, middleware deny-by-default, hạn mức ghi, audit.
- Endpoint OAuth riêng (metadata, authorize + đồng ý + Google + allowlist `sub`, token, refresh xoay vòng bảng riêng, revoke); 2 client đăng ký sẵn.
- Test ca xấu: sub lạ / email chưa verify / viết hoa khác, redirect lạ, PKCE sai/thiếu, `resource` sai, login CSRF (bắt đầu flow ở trình duyệt khác), refresh dùng lại, đồng ý bị nhúng iframe.

**Bước 3 — MCP server lõi (test local)**
- SDK `mcp` 2.x (`MCPServer`). Tool đợt 1: `list_tasks`, `get_task`, `add_devlog`, `tick_checklist`, `get_plan`.
- Verifier tự viết như trên; kiểm scope từng tool; rate limit; Origin/Host; giới hạn kích thước; audit; làm sạch output.
- Test bằng client MCP thật + ca xấu: không token, sai scope/aud/iss/typ/alg (`none`, HS256), hết hạn, `sub` khác, token A gọi API thường, input lớn, id lạ, vượt rate limit, Origin lạ, nội dung task chứa chỉ thị.

**Bước 4 — Đưa lên VPS**
- systemd (user riêng, hardening), nginx (route `/mcp`, `/mcp-oauth`, `/.well-known/*`, streaming, giới hạn), secret qua `LoadCredential`, cấu hình Cloudflare cho `www.tungle.uk`.
- Kiểm: không lộ cổng nội bộ, header bảo mật, log đúng, kill switch (cả nginx 503) hoạt động.

**Bước 5 — Nối vào Claude, thử thật**
- Thêm connector, thử iPhone + Claude Code: đọc task, ghi devlog vào task nháp, bị từ chối khi sai quyền, thử kill switch.

**Bước 6 — Mở rộng**
- Thêm tool (comment, project, link) theo đợt có test; finance chỉ khi thật cần và scope riêng, mặc định tắt. Có thể đóng gói plugin quy trình.

## Rủi ro / việc cần canh

- Endpoint công khai: lỗi xác thực = lộ dữ liệu → test ca xấu trước khi mở.
- Sửa kiểm iss/aud có thể làm hỏng client hiện có → sau cờ cấu hình, thử kỹ.
- Cloudflare có thể chặn/cắt stream của Anthropic → Bước 0.
- App iOS có thể không hỗ trợ đầy đủ connector → Bước 0; dự phòng Safari/claude.ai hoặc chỉ Claude Code.
- Prompt injection qua connector khác → các biện pháp trên + hướng dẫn dùng.
- Mỗi bước dừng chờ Tung duyệt.

## Việc Tung cần kiểm khi có thời gian (AI chưa tự xác minh được)

1. **Cổng 1433 (SQL Server) có truy cập được từ internet không?** VPS có `ufw` tắt và 1433 nghe `0.0.0.0`; ghi chú cũ nói đã chặn ở phía nhà cung cấp. Kiểm bằng một công cụ quét cổng từ mạng khác, hoặc trong bảng điều khiển firewall của nhà cung cấp VPS. Nếu mở: nên đóng ngay.
2. **Cloudflare dashboard** cho `www.tungle.uk`: proxy bật chưa, có Bot Fight Mode/WAF nào có thể chặn Anthropic, cài đặt timeout/WebSocket.
3. Bản iOS của app Claude: có mục Connectors / thêm custom connector không.

## Làm xong khi

Từ iPhone (máy ở nhà tắt), nói "ghi devlog task N …" và nó ghi đúng; mọi ca xấu bị từ chối; có audit log; không secret nào trong repo; kill switch đã thử (kể cả nginx 503); token MCP không dùng được trên API thường.

## Việc để sau / khi tiện

- Siết đăng ký tự do trong product; tách authorization server thành tiến trình riêng.
