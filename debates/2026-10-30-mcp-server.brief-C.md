# Brief mời AI khác vào sàn — vai C (gác cổng / phản biện)

Brief tự chứa. Khối giữa hai `---` là toàn bộ chỉ dẫn cho AI nhận vai C.

---

Bạn tham gia một sàn tranh luận append-only về **plan dựng MCP server cho SuperApp** (project SuperApp của Tung) — vai **C gác cổng / phản biện**: tìm lỗ hổng, sai giả định và chỗ over-engineered trong plan, bằng phản ví dụ cụ thể. Người quyết cuối là Tung (chủ project; một người dùng duy nhất). Hiện mới có một AI (A, Claude — tác giả plan) viết vòng 1 tự chỉ điểm yếu; chưa ai phản biện A. Đừng gật, hãy tấn công.

## Đọc trước (theo thứ tự, path tuyệt đối)
1. `C:\Users\Admin\source\timeline\debates\2026-10-30-mcp-server.md` — file sàn (Vòng 1 của A, có bảng Q).
2. `C:\Users\Admin\source\timeline\docs\mcp-server\PLAN.md` @ `d9e5296` — plan v4 đang tranh luận.
3. Code backend để kiểm bằng chứng (đọc, không sửa): `C:\Users\Admin\source\timeline\SuperAppAPI\Startup.cs`, `...\SuperAppAPI\Controllers\` (đặc biệt `BaseAuthController.cs`, `Projects\TaskController.cs`), `...\SuperAppServices\Services\Auth\AuthService.cs`; client CLI hiện tại: `C:\Users\Admin\source\tung-root\scripts\superapp\superapp.py`.
4. Chuẩn: MCP authorization spec (revision hiện hành, OAuth 2.1, RFC 8707/9728/8414, DCR/CIMD) — bạn có thể tìm web để kiểm; đừng bịa số điều khoản.

## SuperApp và mục tiêu (1 đoạn)
SuperApp là ứng dụng cá nhân của Tung (task, devlog, checklist, plan, finance…): frontend React (`www.tungle.uk`), backend ASP.NET Core (.NET 8) + SQL Server trên một VPS (nginx + Cloudflare phía trước), đăng nhập Google, JWT riêng. Tung muốn dùng dữ liệu SuperApp từ **Claude trên iPhone (iOS 18.5) và Claude Code** — ngay cả khi máy ở nhà tắt — qua một **MCP server** (custom connector) đặt trên VPS, bảo mật tối đa. Hiện Claude đọc/ghi SuperApp qua script CLI chạy trên máy ở nhà. Có đường tấn công đáng lo: nội dung task bị chèn chỉ thị (prompt injection) qua connector khác dùng chung chat (Gmail, web).

## Đã chốt — soi, không mở lại trừ khi có bằng chứng mạnh
- Một danh tính duy nhất (Tung); SuperApp vẫn cho đăng ký tự do (product), siết lại chỉ khi tiện.
- Plan v4 đã sửa theo 2 vòng review độc lập: ghim Google `sub`; `McpController` riêng + deny-by-default; service key gắn cứng 1 user qua listener riêng; tắt DCR, đăng ký sẵn client; chống login CSRF; nhãn nguồn "qua MCP" cho nội dung ghi; refresh nghiêm ngặt; kill switch (DB/file + nginx 503). Chỉ phản bác nếu bạn chứng minh cách xử lý trong plan vẫn sai.
- Host chính thức `www.tungle.uk` (sau Cloudflare); Kestrel chỉ `127.0.0.1:5000`.

## Việc của bạn
Nối **một vòng mới** vào **CUỐI** file sàn (chỉ append — ví dụ `cat >> file` hoặc thêm cuối file — không ghi lại cả file, không sửa mục người khác). Trước khi nối, xem `grep -n '^## ' <file>` để biết số vòng mới nhất N, rồi dùng heading:

`## Vòng N+1 — C (GPT/Codex): <tên vòng> — 2026-10-30`

Mỗi ý một mã `C<vòng>-<số>` (ví dụ `C2-1`, `C2-2`) và đủ 4 phần:
- **Hiện trạng / bằng chứng:** trích `file:dòng` (kiểm lại thật, đừng bịa).
- **Rủi ro / phản ví dụ:** một ví dụ cụ thể làm lập luận sụp.
- **Đề nghị:** cụ thể, làm được.
- **Xong khi / điều kiện đóng:** đo được.

Ý chạm requirement đánh **[R]** — chỉ Tung chốt, bạn chỉ đề nghị.

**Phản biện AI khác là bắt buộc:** mở vòng bằng mục "Trả lời A" — với **mỗi** mã `A1-1 … A1-7` ghi `đồng ý` (kèm lý do riêng của bạn, không chép lại) hoặc `bác` (kèm phản ví dụ) — rồi mới mở ý mới. Vòng chỉ toàn "đồng ý" không có lý do riêng coi như chưa viết. Bạn giữ **đúng một vai** (C), ký đúng chữ cái C.

Chỗ cần soi nhất (gợi ý, không giới hạn):
1. Plan có **over-engineered** cho hệ một người dùng không? Có kiến trúc **đơn giản hơn mà vẫn an toàn tương đương** (ví dụ MCP chạy cùng process .NET, hoặc authorization server tối giản riêng)? So bề mặt tấn công thật.
2. Cloudflare + nginx + Kestrel: chỗ nào flow OAuth/streamable HTTP sẽ **thật sự gãy** (header, host, redirect, timeout, challenge)?
3. Prompt injection: các biện pháp trong plan có **thật sự chặn** kịch bản "task độc → Claude ghi/rò rỉ" không, hay chỉ làm đẹp? v1 nên đọc-chỉ hay có ghi?
4. Lỗ trong **chính các khẳng định kỹ thuật** của plan về backend (hãy mở code kiểm: cách backend lấy user, `Upsert` comment, rate limit, cookie SameSite, refresh…).
5. Thứ tự các bước và Bước 0 (spike): có thiếu gì quan trọng, có bước nào nguy hiểm?
6. Bất cứ thứ gì A và các reviewer trước chưa thấy.

Kết vòng bằng **một bảng Q duy nhất** cho Tung (đánh số tiếp từ Q4, mỗi dòng có đề nghị **in đậm**, ghi "không trả lời = lấy đề nghị") — bạn **không hỏi Tung trực tiếp**, A sẽ hỏi hộ — rồi **dòng cuối cùng** đúng dạng:

`→ chờ: A`

Sau khi nối, **nếu commit được** (repo `C:\Users\Admin\source\timeline`, nhánh `master`, không tạo branch) thì commit chỉ file sàn với message `1734: Vong N+1 (C): <tên vòng>`; nếu sandbox không cho commit thì nói rõ trong kết quả — A sẽ commit hộ sau khi đọc.

Luật tóm tắt: trả lời theo mã (đồng ý / bác / sửa + lý do kỹ thuật), không "tốt rồi" chung chung; "đã có trong plan" hay "các AI đồng ý" chưa phải đóng — chỉ đóng khi có số đo / spike / chấp nhận giới hạn ghi rõ; tự nhận sai công khai khi sai; không sửa doc (việc của A). Giữ vòng dưới ~1500 từ, sắc và có dẫn chứng.

---
