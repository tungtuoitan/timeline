/*
**Fullpath:** /api-tests/_lib/task.js
*/
'use strict';

/**
 * task.js — `/api/task` (TaskController -> TaskService (OwnershipGuard) -> TaskRepository).
 */

const { RUN_ID } = require('./config');

/**
 * Upsert theo lô (`POST /api/task`). Cả lô chạy trong 1 transaction: ownership/validate fail ở
 * BẤT KỲ phần tử nào -> không phần tử nào được ghi.
 *
 * Update (`id > 0`) là FULL replace, trừ: `description` null/không gửi = GIỮ NGUYÊN ("" = xoá);
 * `folderWorkspaceItemId` null = giữ nguyên. Alias cũ `note` = description (0109).
 * Preconditions: `projectId` (và `parentTaskId` nếu có) phải thuộc user — không thì body.status 404.
 * Side-effects: sync keyword task; task trả về được track để cleanup.
 */
async function upsertTasks(s, items) {
  const res = await s.call('POST', '/api/task', items);
  for (const t of res.body?.data ?? []) if (t?.id) s.tracked.tasks.add(t.id);
  return res;
}

/** Tạo 1 task trong project, throw nếu fail. Default BE: type=task, status=open, priority=low. */
async function createTask(s, projectId, overrides = {}) {
  const res = await upsertTasks(s, [{ id: 0, projectId, title: `APITEST ${RUN_ID} task`, ...overrides }]);
  if (!res.body?.success) throw new Error(`createTask fail: ${JSON.stringify(res.body)}`);
  return res.body.data[0];
}

/**
 * `GET /api/task{query}` — filter: projectIds, ids, status (CSV, khớp chính xác), priority (CSV),
 * type, searchText (title/description), deletedAt=null|notNull. Không truyền deletedAt -> trả cả
 * task đã soft delete. Sắp theo orderIndex rồi createdAt. Mỗi task kèm projectStart/EndDate và
 * parentStart/EndDate.
 */
function getTasks(s, query = '') {
  return s.call('GET', `/api/task${query}`);
}

/** `GET /api/task/{id}` -> task, hoặc undefined nếu không có / không thuộc user. */
async function getTask(s, id) {
  const res = await s.call('GET', `/api/task/${id}`);
  return res.body?.data?.[0];
}

/**
 * `PATCH /api/task/{id}` — chỉ ghi field khác null; set null phải qua `clearFields`
 * (description, checklistJson, processJson, customTabsJson, startDate, endDate, parentTaskId).
 * HTTP 404 nếu task không thuộc user / không tồn tại; lỗi validate trả HTTP 200 + body.status 400.
 * Response data[0] kèm ngày giới hạn của project/parent SAU khi patch.
 */
function patchTask(s, id, body) {
  return s.call('PATCH', `/api/task/${id}`, body);
}

/**
 * `DELETE /api/task` — xoá VĨNH VIỄN (1–200 id). Kèm theo: comment, checklist history, flow
 * edge/position, folder + note workspace, keyword. Task con KHÔNG bị xoá, chỉ về parent=null.
 * ⚠️ All-or-nothing: 1 id không tồn tại/không thuộc user -> body.status 404, không xoá gì.
 */
function hardDeleteTasks(s, ids) {
  return s.call('DELETE', '/api/task', { ids });
}

module.exports = { upsertTasks, createTask, getTasks, getTask, patchTask, hardDeleteTasks };
