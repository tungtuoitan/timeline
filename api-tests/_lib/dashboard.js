/*
**Fullpath:** /api-tests/_lib/dashboard.js
*/
'use strict';

/**
 * dashboard.js — homepage tiến bộ tổng hợp (TungRoot #1481):
 *   - `/api/dashboard/activity`, `/api/dashboard/habits` (DashboardController, chỉ đọc)
 *   - tài chính: xem finance.js (#1482)
 * Ngày theo timezone của user (mặc định Asia/Ho_Chi_Minh); tuần bắt đầu thứ Hai.
 */

/**
 * `GET /api/dashboard/activity?from&to&types` — số comment (mặc định devlog,comment,decision) theo
 * (tuần, project) của user. Range mặc định 26 tuần tới hôm nay; from > to / quá 3 năm / type lạ ->
 * HTTP 400.
 */
function getActivity(s, query = '') {
  return s.call('GET', `/api/dashboard/activity${query}`);
}

/**
 * `GET /api/dashboard/habits?from&to&taskIds&excludeTaskIds` — tracker (task `type=repeat`, hoặc
 * đúng các `taskIds` nếu truyền) + entry `track`/`comment` theo ngày. Task của user khác bị bỏ qua
 * im lặng (không lộ tồn tại). Range mặc định 56 ngày; id lạ -> HTTP 400.
 */
function getHabits(s, query = '') {
  return s.call('GET', `/api/dashboard/habits${query}`);
}

module.exports = { getActivity, getHabits };
