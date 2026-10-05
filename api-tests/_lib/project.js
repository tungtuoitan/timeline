/*
**Fullpath:** /api-tests/_lib/project.js
*/
'use strict';

/**
 * project.js — `/api/project` (ProjectController -> ProjectService -> ProjectRepository).
 * Mọi hàm nhận `s` = session (xem dataManager.newSession) làm tham số đầu; facade bind sẵn nên
 * case gọi `a.createProject(...)`.
 */

const { RUN_ID } = require('./config');

/**
 * Upsert theo lô (`POST /api/project`, = `/batch`). `id: 0` = tạo, `id > 0` = FULL replace
 * (field không gửi lấy default của DTO, vd status "open" — muốn giữ thì gửi lại đủ).
 *
 * @returns {Promise<{http, body}>} raw response.
 * Side-effects: project mới -> BE TỰ TẠO 1 workspace 1:1 (ws.workspaces) cùng tên; update có
 *   `workspaceId` -> đổi tên workspace theo. Sync keyword project. Mọi project trả về được
 *   track để cleanup.
 */
async function upsertProjects(s, items) {
  const res = await s.call('POST', '/api/project', items);
  for (const p of res.body?.data ?? []) if (p?.id) s.tracked.projects.set(p.id, p);
  return res;
}

/**
 * Tạo 1 project (status mặc định 'active'), throw nếu không thành công.
 * @returns {Promise<object>} project vừa tạo (`id`, `userId`, `workspaceId`, ...).
 */
async function createProject(s, overrides = {}) {
  const res = await upsertProjects(s, [{ id: 0, name: `APITEST ${RUN_ID} project`, status: 'active', ...overrides }]);
  if (!res.body?.success) throw new Error(`createProject fail: ${JSON.stringify(res.body)}`);
  return res.body.data[0];
}

/** `GET /api/project{query}` — query vd `?ids=1,2&status=active&deletedAt=null`. */
function getProjects(s, query = '') {
  return s.call('GET', `/api/project${query}`);
}

/** `GET /api/project/{id}` — trả raw response (data rỗng nếu không thuộc user). */
function getProjectRes(s, id) {
  return s.call('GET', `/api/project/${id}`);
}

module.exports = { upsertProjects, createProject, getProjects, getProjectRes };
