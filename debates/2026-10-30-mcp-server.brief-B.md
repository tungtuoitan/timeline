# Brief mời AI khác vào sàn — vai B (validator / phản biện độc lập)

Brief tự chứa. Khối giữa hai `---` là toàn bộ chỉ dẫn cho AI nhận vai B.

---

Bạn tham gia một sàn tranh luận append-only về **plan dựng MCP server cho SuperApp** (project SuperApp của Tung) — vai **B validator / phản biện độc lập**. Người quyết cuối là Tung (chủ project; một người dùng duy nhất). Trên sàn đã có 23 vòng giữa A (Claude, tác giả plan) và C (GPT/Codex, phản biện). **Tung đã cho C nghỉ và mời bạn vào** vì sàn đang cho giá trị giảm dần: mỗi vòng C vẫn tìm 3–5 điểm mới nhưng phần lớn đã là chi tiết cài đặt. Bạn **không phải người lặp lại C và không phải người gật với A**. Việc của bạn là nhìn bằng con mắt mới.

## Đọc trước (path tuyệt đối)
1. `C:\Users\Admin\source\timeline\debates\2026-10-30-mcp-server.md` — file sàn (23 vòng; đọc kỹ Vòng 23 của C và Vòng 22 của A, lướt các vòng trước).
2. `C:\Users\Admin\source\timeline\docs\mcp-server\PLAN.md` @ `1a932d6` (plan v13) — doc đang tranh luận.
3. Code backend để kiểm bằng chứng (đọc, không sửa): `C:\Users\Admin\source\timeline\SuperAppAPI\Startup.cs`, `...\SuperAppAPI\Controllers\`, `...\SuperAppServices\Services\Auth\AuthService.cs`, `...\SuperAppServices\Services\Auth\TotpService.cs`, `...\SuperAppServices\Services\Projects\TaskCommentService.cs`, `...\SuperAppDataRepositories\Repositories\Projects\TaskCommentRepository.cs`, `...\SuperAppDataRepositories\Repositories\Projects\TaskRepository.cs`; client CLI: `C:\Users\Admin\source\tung-root\scripts\superapp\superapp.py`.
4. Spec MCP revision 2026-07-28 trên modelcontextprotocol.io (authorization, transports/streamable-http, server/discover) — bạn có thể tra web; đừng bịa điều khoản.

## SuperApp và mục tiêu (1 đoạn)
SuperApp là ứng dụng cá nhân của Tung (task, devlog, checklist, plan, finance…): frontend React (`www.tungle.uk`), backend ASP.NET Core (.NET 8) + SQL Server trên một VPS (nginx + Cloudflare phía trước), đăng nhập Google, JWT riêng. Tung muốn Claude trên **iPhone (iOS 18.5) và Claude Code** đọc/ghi dữ liệu SuperApp qua một **MCP server** (custom connector) đặt trên VPS, ngay cả khi máy ở nhà tắt. Quyết định của Tung (đã chốt, đừng mở lại nếu không có bằng chứng mạnh): một danh tính duy nhất; MCP chạy cùng process ASP.NET; bản đầu có đọc + ghi devlog; TOTP khi cấp quyền; cho đọc cả task nhạy cảm; đóng cửa hạ tầng (cổng DB, origin) trước; cap đọc 256 KiB/trang, 64 KiB/mô tả; quota devlog 50/ngày, 16 KiB/lần.

## Việc của bạn
Nối **một vòng mới** vào **CUỐI** file sàn (chỉ append — ví dụ `cat >> file` — không ghi lại cả file, không sửa mục người khác). Trước khi nối: `grep -n '^## ' <file>` để biết số vòng mới nhất N, rồi dùng heading:

`## Vòng N+1 — B (Claude Fable): <tên vòng> — 2026-10-31`

Bạn **là B** — ký đúng chữ cái B, giữ một vai. Nội dung, theo thứ tự:

**1. Phán quyết độc lập trước (đừng để C dẫn dắt).** Đọc plan v13 và tự trả lời, bằng bằng chứng: (a) plan có **khả thi và đủ đơn giản** cho một người dùng duy nhất không? (b) chỗ nào **over-engineered** (chi phí/độ phức tạp lớn hơn rủi ro được chặn)? (c) chỗ nào **under-engineered** hoặc **thiếu** hẳn? (d) thứ tự các bước có sai không?

**2. Phân loại ~20+ điểm C còn nêu (C2…C23, A đã trả lời).** Lập bảng: mã → **blocker thật (phải sửa plan trước khi code)** / **điều kiện test khi cài đặt (không cần tranh luận thêm)** / **thừa, nên bỏ** — kèm lý do ngắn. Mục tiêu: giúp Tung biết điểm nào thực sự cần quyết và điểm nào chỉ là test.

**3. Trả lời mã của AI khác (bắt buộc, có phản ví dụ khi bác):** ít nhất các mã chưa đóng ở Vòng 23 — `C23-5, C23-8, C23-9, C23-10, C23-11, C23-12` — và các mã A đang nêu (`A22`, đặc biệt `Q19`, `Q20`). Mỗi mã ghi `đồng ý` (lý do riêng của bạn, không chép lại) hoặc `bác` (phản ví dụ cụ thể). **Kiểm lại dẫn chứng code của C và A bằng cách mở file thật** — nếu C hay A trích sai, nói rõ `file:dòng`.

**4. Lỗ mới của riêng bạn** (mã `B<vòng>-<số>`, 4 phần: **Hiện trạng/bằng chứng** `file:dòng`; **Rủi ro/phản ví dụ** cụ thể; **Đề nghị** làm được; **Xong khi** đo được). Ưu tiên cái nghiêm trọng thực tế (mất dữ liệu, lộ dữ liệu, tự sập production, không thể vận hành), không ưu tiên chi tiết tinh tế. Nhìn những thứ mà C hay A có thể thiên lệch bỏ sót: vận hành (cách Tung thật sự dùng, khôi phục sự cố, thời gian thực hiện), quy mô công việc, rủi ro **do chính các biện pháp bảo mật gây ra** (tự khoá Tung ra, tự sập app), rủi ro của việc **không làm gì**.

**5. Đề nghị đóng:** nếu bạn thấy đủ, ghi **"đề nghị ĐÓNG sàn"** kèm **danh sách ngắn** điều kiện còn treo (cái gì phải xong trước khi code, cái gì chuyển thành test). Nếu chưa đủ, nêu **chính xác** điểm nghiêm trọng nào còn lại. Đừng kéo dài vì thói quen.

Cuối vòng: **một bảng Q duy nhất** cho Tung (đánh số tiếp từ Q24, đề nghị **in đậm**, "không trả lời = lấy đề nghị") — bạn **không hỏi Tung trực tiếp**, A sẽ hỏi hộ — rồi **dòng cuối cùng** đúng dạng:

`→ chờ: A`

Giữ vòng dưới ~2000 từ, sắc, có dẫn chứng. Không sửa PLAN.md (việc của A). Sau khi nối, **commit chỉ file sàn** trong repo `C:\Users\Admin\source\timeline` (nhánh `master`, không tạo branch) với message `1734: Vong N+1 (B, Fable): <tên vòng>` kết thúc bằng dòng `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`; nếu không commit được thì nói rõ trong kết quả — A sẽ commit hộ.

Luật tóm tắt: trả lời theo mã (đồng ý / bác / sửa + lý do kỹ thuật), không "tốt rồi" chung chung; "đã có trong plan" hay "các AI đồng ý" chưa phải đóng; tự nhận sai công khai khi sai; không hỏi Tung trực tiếp; không gật chỉ vì hai AI trước đã đồng ý với nhau.

---
