# Kết quả chạy api-tests

**Fullpath:** /api-tests/RESULTS.md

> File SINH TỰ ĐỘNG bởi `node run-tests.js` (chạy cả bộ) — không sửa tay.

- Thời điểm: 2026-10-05T15:06:24.655Z
- BE: http://localhost:5000
- Kết quả: **74/74 pass**

| Kết quả | Case | File | Lỗi |
|---|---|---|---|
| ✅ | Flow01#1: tạo project -> id>0, userId lấy từ token (bỏ qua userId client gửi), workspace tự tạo | flows/01-project/01-1-create-validate.test.js |  |
| ✅ | Flow01#2.1/#2.2/#2.4: thiếu name, name > 255, body [] -> HTTP 400 | flows/01-project/01-1-create-validate.test.js |  |
| ✅ | Flow01#2.3: tạo mới kèm deletedAt -> body.status 400 | flows/01-project/01-1-create-validate.test.js |  |
| ✅ | Flow01#2.5: không có token -> HTTP 401 | flows/01-project/01-1-create-validate.test.js |  |
| ✅ | Flow01#3: update name/description/status/ngày -> GET trả giá trị mới, ngày YYYY-MM-DD | flows/01-project/01-2-update-filter.test.js |  |
| ✅ | Flow01#4: batch 3 project trong 1 request -> trả đủ 3 | flows/01-project/01-2-update-filter.test.js |  |
| ✅ | Flow01#5: filter status CSV (#5.1), ids (#5.2), searchText name/description (#5.3), mới tạo đứng trước (#5.4) | flows/01-project/01-2-update-filter.test.js |  |
| ✅ | Flow01#6/#8: GET /api/project/{id} trả đúng 1; status open/planned (0095) lưu được | flows/01-project/01-2-update-filter.test.js |  |
| ✅ | Flow01#7: soft delete (#7.1) rồi restore (#7.2) qua upsert deletedAt | flows/01-project/01-2-update-filter.test.js |  |
| ✅ | Flow02#1: tạo task tối thiểu -> default type/taskType/status/priority/isMilestone; createdAt có offset (#1.1) | flows/02-task-upsert/02-1-create-validate.test.js |  |
| ✅ | Flow02#2.1/#2.2: thiếu title, title > 500 -> HTTP 400 | flows/02-task-upsert/02-1-create-validate.test.js |  |
| ✅ | Flow02#2.3/#2.4/#2.5: project không tồn tại 404, deletedAt khi tạo 400, id không tồn tại 404 | flows/02-task-upsert/02-1-create-validate.test.js |  |
| ✅ | Flow02#3: lô có 1 phần tử lỗi -> cả lô bị từ chối, phần tử hợp lệ cũng không được tạo | flows/02-task-upsert/02-1-create-validate.test.js |  |
| ✅ | Flow02#4: update ghi đè field; description không gửi = giữ (#4.1), alias note (#4.3), &quot;&quot; = xoá (#4.2) | flows/02-task-upsert/02-2-update-fields.test.js |  |
| ✅ | Flow02#5: checklistJson/processJson/customTabsJson + isMilestone round-trip nguyên vẹn | flows/02-task-upsert/02-2-update-fields.test.js |  |
| ✅ | Flow02#6/#10: task con kèm ngày giới hạn của cha + project; ngày YYYY-MM-DD không lệch múi giờ | flows/02-task-upsert/02-2-update-fields.test.js |  |
| ✅ | Flow02#7: soft delete rồi restore qua upsert deletedAt | flows/02-task-upsert/02-3-soft-delete-filter.test.js |  |
| ✅ | Flow02#8: filter status khớp chính xác (#8.1), priority (#8.2), type (#8.3), searchText (#8.4), projectIds (#8.5), sắp orderIndex (#8.6) | flows/02-task-upsert/02-3-soft-delete-filter.test.js |  |
| ✅ | Flow02#9: GET /api/task/{id} không tồn tại -> success, data rỗng | flows/02-task-upsert/02-3-soft-delete-filter.test.js |  |
| ✅ | Flow03#1: patch chỉ status -> các field khác giữ nguyên | flows/03-task-patch/03-1-partial-clear-validate.test.js |  |
| ✅ | Flow03#2/#10: patch nhiều field cùng lúc; alias note ghi vào description | flows/03-task-patch/03-1-partial-clear-validate.test.js |  |
| ✅ | Flow03#3.1: clearFields description/startDate/endDate/parentTaskId/checklistJson -> null | flows/03-task-patch/03-1-partial-clear-validate.test.js |  |
| ✅ | Flow03#3.2/#4.1/#4.2/#4.3: clear field cấm, title rỗng, status rỗng, cha = chính nó -> body.status 400, không ghi gì | flows/03-task-patch/03-1-partial-clear-validate.test.js |  |
| ✅ | Flow03#4.4/#4.5: task không tồn tại -> HTTP 404; id = 0 -> HTTP 400 | flows/03-task-patch/03-1-partial-clear-validate.test.js |  |
| ✅ | Flow03#5/#6: chuyển project / gán cha -> response kèm ngày giới hạn của project mới / cha | flows/03-task-patch/03-2-move-dates-status.test.js |  |
| ✅ | Flow03#7: ngày ISO có Z -> quy về ngày theo timezone user (17:30Z = 00:30 +07 hôm sau) | flows/03-task-patch/03-2-move-dates-status.test.js |  |
| ✅ | Flow03#8/#9 (1468): completed khi checklist chưa tick hết được phép; tick hết process không tự đổi status | flows/03-task-patch/03-2-move-dates-status.test.js |  |
| ✅ | Flow04#1: xoá task có comment + task con -> task mất hẳn, comment mất (#1.1), con còn với parent=null (#1.2) | flows/04-task-hard-delete/04-1-hard-delete.test.js |  |
| ✅ | Flow04#2: xoá nhiều task 1 lần | flows/04-task-hard-delete/04-1-hard-delete.test.js |  |
| ✅ | Flow04#3/#4: ids rỗng hoặc > 200 id -> HTTP 400 | flows/04-task-hard-delete/04-1-hard-delete.test.js |  |
| ✅ | Flow04#6: xoá task có comment kèm reply (kể cả reply của comment đã soft delete) -> thành công | flows/04-task-hard-delete/04-1-hard-delete.test.js |  |
| ✅ | Flow04#5: lẫn 1 id không tồn tại -> 404, task hợp lệ trong lô vẫn còn | flows/04-task-hard-delete/04-1-hard-delete.test.js |  |
| ✅ | Flow05#1: tạo không gửi type -> type=comment, userId đúng, occurredAt null, createdAt có offset, message &quot;Comment created&quot; (#1.1) | flows/05-task-comment/05-1-create-type-filter.test.js |  |
| ✅ | Flow05#2/#3: type devlog/decision/track lưu đúng; type lạ (#2.1), content rỗng/khoảng trắng (#3) bị từ chối | flows/05-task-comment/05-1-create-type-filter.test.js |  |
| ✅ | Flow05#4/#9: GET lọc type CSV; type lạ 400 (#4.1); task không tồn tại 404 (#9) | flows/05-task-comment/05-1-create-type-filter.test.js |  |
| ✅ | Flow05#5: occurredAt có offset lưu đúng instant (#5.1), không offset -> HTTP 400 (#5.2), sắp theo occurredAt ?? createdAt (#5.3) | flows/05-task-comment/05-1-create-type-filter.test.js |  |
| ✅ | Flow05#6.1/#6.2: sửa content giữ nguyên type + occurredAt; sửa với taskId lệch -> 404 | flows/05-task-comment/05-2-edit-reply-delete.test.js |  |
| ✅ | Flow05#7/#8/#6.3: reply cùng task OK, cha khác task 404; xoá cha ẩn luôn reply; sửa comment đã xoá 404 | flows/05-task-comment/05-2-edit-reply-delete.test.js |  |
| ✅ | Flow05#8.1: xoá comment không tồn tại -> 404 | flows/05-task-comment/05-2-edit-reply-delete.test.js |  |
| ✅ | Flow06#1/#2: B đọc project/task của A -> rỗng | flows/06-ownership/06-1-cross-user-isolation.test.js |  |
| ✅ | Flow06#3: B sửa project A -> 404 và A không đổi; B gắn workspace của A (#3.1) -> 404 | flows/06-ownership/06-1-cross-user-isolation.test.js |  |
| ✅ | Flow06#4/#5/#6: B tạo task vào project A, ghi đè task A, tạo con của task A -> 404, A không đổi | flows/06-ownership/06-1-cross-user-isolation.test.js |  |
| ✅ | Flow06#7: B PATCH task A -> HTTP 404; B chuyển task mình sang project A (#7.1) / gán cha task A (#7.2) -> 404 | flows/06-ownership/06-1-cross-user-isolation.test.js |  |
| ✅ | Flow06#8: B hard-delete task A -> 404, task A còn | flows/06-ownership/06-1-cross-user-isolation.test.js |  |
| ✅ | Flow06#9: B đọc (#9.1) / comment (#9.2) / sửa (#9.3) / xoá (#9.4) comment trên task A -> bị chặn, comment A nguyên vẹn | flows/06-ownership/06-1-cross-user-isolation.test.js |  |
| ✅ | Flow07#1: thêm link vào task chưa có folder -> BE tạo folder, link nằm trong đó | flows/07-links/07-1-task-links.test.js |  |
| ✅ | Flow07#2: link hiện ngay trong tree/v2 (không đợi cache) | flows/07-links/07-1-task-links.test.js |  |
| ✅ | Flow07#3: url không hợp lệ -> 400, không tạo gì | flows/07-links/07-1-task-links.test.js |  |
| ✅ | Flow07#4: không truyền name -> name = host + path | flows/07-links/07-1-task-links.test.js |  |
| ✅ | Flow07#5: gắn item có sẵn (note) của cùng workspace, gắn lại không trùng | flows/07-links/07-1-task-links.test.js |  |
| ✅ | Flow07#6: link tạo bằng workspace batch trong folder task cũng có trong task links | flows/07-links/07-1-task-links.test.js |  |
| ✅ | Flow07#7: xoá link -> link trong folder bị soft delete, item có sẵn chỉ bỏ gắn | flows/07-links/07-1-task-links.test.js |  |
| ✅ | Flow07#8: đổi tên + url link qua PATCH /api/file/{id} | flows/07-links/07-2-project-links-file.test.js |  |
| ✅ | Flow07#9: link project nằm trong folder Links ở gốc workspace, xoá = soft delete | flows/07-links/07-2-project-links-file.test.js |  |
| ✅ | Flow07#10: hard delete task có link -> folder + link biến mất khỏi tree | flows/07-links/07-2-project-links-file.test.js |  |
| ✅ | Flow07#11: B không đọc/thêm/xoá link, không sửa file, không batch vào workspace của A | flows/07-links/07-3-ownership.test.js |  |
| ✅ | Flow08#1: activity đếm devlog/comment/decision theo (tuần, project), bỏ track | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#1.1: ranh giới tuần theo giờ VN (T2 00:30 +07 thuộc tuần mới) | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#1.2: types=track chỉ đếm track | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#1.3: from > to, type lạ -> HTTP 400 | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#2: habits mặc định chỉ repeat, entry track+comment theo ngày VN | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#2.1: taskIds= task thường | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#2.2: excludeTaskIds bỏ tracker | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#2.3: taskIds=abc -> HTTP 400 | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#3: user B không thấy activity/tracker của A | flows/08-dashboard/08-1-activity-habits.test.js |  |
| ✅ | Flow08#4: POST lô giao dịch -> chèn hết; gửi lại y nguyên -> unchanged, không nhân đôi | flows/08-dashboard/08-2-finance.test.js |  |
| ✅ | Flow08#4.1: sửa tay category rồi nguồn gửi lại số mới -> cập nhật số, giữ category | flows/08-dashboard/08-2-finance.test.js |  |
| ✅ | Flow08#4.2: dữ liệu sai -> HTTP 400, báo đúng dòng | flows/08-dashboard/08-2-finance.test.js |  |
| ✅ | Flow08#5: GET lọc kind / account / uncategorized / from-to | flows/08-dashboard/08-2-finance.test.js |  |
| ✅ | Flow08#6: summary — tài sản ròng theo ngày (giá carry-forward), tháng tách chi tiêu/đầu tư | flows/08-dashboard/08-2-finance.test.js |  |
| ✅ | Flow08#6.1: summary interval=week / tham số sai -> HTTP 400 | flows/08-dashboard/08-2-finance.test.js |  |
| ✅ | Flow08#7: giá — latest theo (asset, quote); giá <= 0 -> HTTP 400 | flows/08-dashboard/08-2-finance.test.js |  |
| ✅ | Flow08#8: user B không thấy / không sửa / không xoá được giao dịch của A | flows/08-dashboard/08-2-finance.test.js |  |
| ✅ | Flow08#9: DELETE -> biến mất khỏi list; xoá lại -> HTTP 404 | flows/08-dashboard/08-2-finance.test.js |  |
