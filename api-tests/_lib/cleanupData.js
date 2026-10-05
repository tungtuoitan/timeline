/*
**Fullpath:** /api-tests/_lib/cleanupData.js
*/
'use strict';

/**
 * cleanupData.js — dọn data 1 session tạo ra, qua API (gọi trong `after()` của mỗi file test).
 *
 * - Task: hard delete. CHỈ xoá id còn tồn tại (hỏi lại BE trước) — DELETE /api/task là
 *   all-or-nothing, lẫn 1 id case đã tự xoá là cả lô bị từ chối (bug cũ: còn sót task).
 * - Project: soft delete (API chưa có hard delete project).
 * - User + workspace + project đã soft delete + history temporal: API KHÔNG xoá được -> dọn định
 *   kỳ bằng `_scripts/cleanup-test-data-by-prefix.sql` (đó cũng là đường dọn khi process crash
 *   giữa chừng: mọi data test đều thuộc user `apitest+%@test.local`, nên email chính là "journal").
 */

async function cleanupSession(s) {
  for (const id of s.tracked.finTransactions ?? []) await s.call('DELETE', `/api/finance/transactions/${id}`).catch(() => {});

  const tracked = [...s.tracked.tasks];
  for (let i = 0; i < tracked.length; i += 200) {
    const chunk = tracked.slice(i, i + 200);
    const res = await s.call('GET', `/api/task?ids=${chunk.join(',')}`).catch(() => null);
    const alive = (res?.body?.data ?? []).map((t) => t.id);
    if (alive.length) await s.call('DELETE', '/api/task', { ids: alive }).catch(() => {});
  }

  const now = new Date().toISOString();
  const projects = [...s.tracked.projects.values()].map((p) => ({
    id: p.id, name: p.name || 'APITEST', status: p.status || 'active', workspaceId: p.workspaceId ?? undefined, deletedAt: now,
  }));
  if (projects.length) await s.call('POST', '/api/project', projects).catch(() => {});
}

module.exports = { cleanupSession };
