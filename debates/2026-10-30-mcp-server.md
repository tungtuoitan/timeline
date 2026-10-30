# SuperApp — sàn tranh luận: MCP server để dùng SuperApp từ Claude trên điện thoại (2026-10-30)

Đối tượng: `docs/mcp-server/PLAN.md` @ `d9e5296` (plan v4; task SuperApp #1734).
Mục tiêu: lôi ra mọi vấn đề nghiêm trọng / rủi ro còn sót của plan, **kể cả ở requirement** (phi lý, xung đột, thiếu, over-engineered cho hệ một người dùng).

## Người tham gia và luật

| Tên | Là ai | Vai |
|---|---|---|
| **A** | Claude (Sonnet 5.5), session "Tổ chức Timeline" — tác giả plan | đề xuất, sửa plan, commit, ghi hash; **đồng thời là người phát ngôn với Tung** (sàn chỉ có 2 AI nên Tung chỉ định A hỏi Tung; không có B) |
| **B** | (trống — chưa có validator riêng) | — |
| **C** | GPT / Codex — gác cổng, **phản biện** | góc nhìn độc lập, phản ví dụ + điều kiện đóng; kiểm diff của A; không hỏi Tung trực tiếp |
| **Tung** | chủ | quyết mọi mục [R] — **không đọc file**; nghe A (người phát ngôn) |

Luật: mỗi AI một vai cố định, ký đúng chữ cái; mỗi vòng mở bằng **trả lời mã của AI khác** (đồng ý có lý do riêng / bác có phản ví dụ) rồi mới mở ý mới — vòng chỉ gật coi như chưa viết; mỗi ý một mã `<bên><vòng>-<số>`; [R] = chỉ Tung chốt; trạng thái OPEN → ĐÃ CHỐT CÁCH XỬ LÝ (hash) → ĐÃ KIỂM CHỨNG / CHẤP NHẬN GIỚI HẠN; **chỉ nối vào cuối file** (`cat >>`), không sửa mục người khác; dẫn chứng `file:dòng`, người nhận kiểm lại; danh sách chờ Tung gom thành một bảng Q.
Giao tiếp: file này là kênh duy nhất. **Mỗi vòng kết thúc bằng dòng `→ chờ: <A | C | Tung | không>`.**

## Đã chốt trước khi mở sàn (để C khỏi lặp)
- Mục đích: dùng SuperApp (task, devlog, checklist, plan) từ Claude trên iPhone (iOS 18.5) và Claude Code qua MCP server trên VPS, khi máy ở nhà tắt. (Tung)
- Một danh tính duy nhất dùng MCP (Tung); SuperApp vẫn cho đăng ký tự do (product), siết lại chỉ khi tiện. (Tung)
- Authorization Server: đặt trong backend (cô lập) — "cái nào tiện thì làm", chưa phải quyết định cứng. (Tung)
- Host chính thức `www.tungle.uk` (sau Cloudflare); Kestrel chỉ nghe `127.0.0.1:5000`; `ufw` tắt; `1433` nghe `0.0.0.0` (chưa xác minh từ ngoài — Tung kiểm sau). (A đã kiểm trên VPS)
- Backend hiện: JWT HS256 `Jwt:Key`, `ValidateIssuer/Audience=false` (`SuperAppAPI/Startup.cs:60-71`); mọi controller `[Authorize]` cấp lớp, user lấy từ claim (`SuperAppAPI/Controllers/BaseAuthController.cs:8-12`).
- Đã có 2 vòng review độc lập (2 agent Opus, không phải thành viên sàn); kết quả đã vào plan v4: ghim Google `sub`, `McpController` riêng + deny-by-default, listener riêng cho service key, tắt DCR, chống login CSRF, nhãn nguồn "qua MCP", refresh nghiêm ngặt, kill switch. C **không** cần lặp những điểm này trừ khi có bằng chứng mạnh rằng cách xử lý trong plan vẫn sai.

---

## Vòng 1 — A (Claude Sonnet 5.5): mở sàn, tự chỉ điểm yếu — 2026-10-30

A chưa có mã của AI khác để trả lời (vòng đầu). A tự chỉ ra 7 chỗ nghi ngờ nhất của plan để C tấn công trước; C đừng chỉ tấn công những chỗ này mà hãy tìm cả chỗ A chưa thấy.

### A1-1 [R] Có cần authorization server trong backend không? Có thể quá tay cho hệ một người dùng
- **Hiện trạng:** Plan v4 Bước 2–3 thêm OAuth 2.1 server (metadata, authorize + consent, token, refresh xoay vòng, revoke, bảng riêng, khoá ký riêng) vào backend .NET.
- **Phương án thay thế:** MCP server tự làm authorization server **tối giản** cho đúng một người: một client đăng ký sẵn, đăng nhập Google (hoặc TOTP đã có ở backend), cấp token ngắn hạn; backend SuperApp không đổi gì ngoài `McpController` + service key.
- **Rủi ro:** Cách B đặt bề mặt OAuth vào app chính có dữ liệu tài chính/TOTP; cách thay thế đặt vào một tiến trình nhỏ có thể đập đi làm lại. Ngược lại tự làm AS = tự gánh lỗi OAuth.
- **Đề nghị của A:** giữ B (backend) chỉ nếu C không chứng minh được cách thay thế an toàn hơn; còn lại chọn cách ít code mới nhất sau Bước 0.
- **Xong khi:** C nêu phương án đúng + phản ví dụ cho cái còn lại; A cập nhật plan, ghi hash.

### A1-2 Cloudflare đứng trước có thể phá cả flow
- **Hiện trạng:** `curl -I https://www.tungle.uk/` → `server: cloudflare`; nginx có `real_ip_header CF-Connecting-IP`.
- **Rủi ro:** Bot Fight / WAF / challenge chặn máy chủ Anthropic gọi `/mcp`; timeout ~100s cắt stream; OAuth callback bị challenge.
- **Đề nghị:** dùng subdomain DNS-only (`mcp.tungle.uk`, chứng chỉ riêng) tránh Cloudflare? Đổi lại lộ IP gốc và mất bảo vệ DDoS. Hay giữ proxy + rule bỏ qua cho `/mcp*`?
- **Xong khi:** Bước 0 đo thực tế (stream 5 phút, request từ Anthropic) và chọn.

### A1-3 Service key qua unix socket/listener riêng — có đáng độ phức tạp?
- **Hiện trạng:** plan bắt Kestrel mở thêm listener riêng (unix socket 0600) miễn HTTPS redirect; scheme `McpService` ngoài policy mặc định; middleware 403 mọi endpoint không `[McpAllowed]`.
- **Rủi ro:** thêm cấu hình Kestrel + nginx dễ sai (Kestrel hiện `ASPNETCORE_URLS=http://localhost:5000`, chạy `www-data`). Nếu sai thì chính cơ chế "chỉ localhost" lại thành lỗ.
- **Đề nghị thay thế:** MCP server chạy **cùng process ASP.NET** (MCP C# SDK) — khỏi cần service key và socket, gọi thẳng service layer qua DI. Đổi lại viết MCP bằng C# thay vì Python và gắn với backend chính.
- **Xong khi:** C so sánh hai kiến trúc, nêu cái nào có bề mặt tấn công nhỏ hơn.

### A1-4 [R] v1 có nên là CHỈ-ĐỌC?
- **Hiện trạng:** plan v4 đợt 1 có `add_devlog` và `tick_checklist` (ghi).
- **Rủi ro:** prompt injection qua connector khác (Gmail/web) có thể khiến Claude ghi dữ liệu độc vào devlog; sau đó Claude Code (có shell) đọc lại. Các biện pháp (nhãn nguồn, cắt độ dài, bỏ URL, "luôn hỏi") chỉ giảm nhẹ.
- **Đề nghị của A:** v1 chỉ-đọc + một tool ghi duy nhất `add_devlog` tạo mới, hạn mức nhỏ; `tick_checklist` để đợt sau. Hay hoàn toàn chỉ-đọc ở v1?
- **Xong khi:** Tung chốt mức quyền v1; C chỉ ra kịch bản tấn công vượt các biện pháp giảm nhẹ nếu có.

### A1-5 `sub` ghim cứng — kịch bản mất quyền truy cập / khôi phục
- **Rủi ro:** nếu Google account của Tung bị khoá/đổi, MCP khoá hẳn; đồng thời là điểm tập trung (Google account bị chiếm = chiếm MCP).
- **Đề nghị:** có đường khôi phục (cấu hình `sub` mới qua SSH) và cân nhắc lớp thứ hai (TOTP đã có sẵn trong backend `auth.user_totp`) cho authorize MCP.
- **Xong khi:** C nêu có cần lớp thứ hai không, có nên dùng TOTP của backend không.

### A1-6 Bước 0 spike: đủ an toàn và đủ thông tin chưa?
- **Hiện trạng:** spike = server giả không xác thực, dữ liệu giả, subdomain tạm; OAuth thử riêng.
- **Rủi ro:** subdomain tạm công khai không xác thực = bề mặt DNS/Cloudflare thêm; kết quả spike không xác thực có thể không phản ánh hành vi khi có OAuth (app iOS có thể xử lý OAuth khác).
- **Đề nghị:** spike tối thiểu, tự huỷ sau N giờ (systemd timer), URL khó đoán; thêm một bước thử OAuth giả chỉ với khoá test.
- **Xong khi:** C chỉ ra điều gì spike phải đo mà plan chưa liệt kê.

### A1-7 Việc ngoài plan nhưng nghiêm trọng hơn: cổng 1433 có thể mở ra internet
- **Hiện trạng:** `ss -tlnp` trên VPS: `sqlservr` nghe `0.0.0.0:1433` và `*:1433`; `ufw status: inactive`; `iptables -P INPUT ACCEPT`. Ghi chú cũ nói "đã chặn" (có thể ở firewall nhà cung cấp VPS). A **không xác minh được từ ngoài** (mạng máy A trả kết nối giả cho mọi cổng).
- **Rủi ro:** nếu mở: mật khẩu `sa` bị brute-force = mất toàn bộ DB, bất kể MCP an toàn thế nào.
- **Đề nghị:** đưa vào danh sách Tung kiểm sớm nhất; plan thêm bước "đóng 1433 trừ localhost" (bind SQL Server về 127.0.0.1 hoặc bật ufw + deny) **trước** MCP.
- **Xong khi:** Tung xác minh từ mạng khác; nếu mở thì đóng.

---

### Bảng Q (A gom, chưa hỏi Tung — chờ C phản biện trước)

| # | Quyết gì | Lựa chọn (đề nghị **in đậm**) | Nguồn |
|---|---|---|---|
| Q1 | v1 có quyền ghi không? | **Chỉ-đọc + `add_devlog` tạo mới** / hoàn toàn chỉ-đọc / đủ ghi như plan | A1-4 |
| Q2 | Authorization server đặt ở đâu? | **Quyết sau Bước 0 theo cái ít code mới nhất** / backend / MCP server riêng | A1-1 |
| Q3 | Có lớp xác thực thứ hai (TOTP) khi cấp token MCP không? | **Có, dùng TOTP sẵn có** / không | A1-5 |

Không trả lời = lấy đề nghị. (A sẽ hỏi Tung sau khi C phản biện.)

→ chờ: C
