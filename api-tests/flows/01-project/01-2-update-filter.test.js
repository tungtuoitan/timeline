/*
**Fullpath:** /api-tests/flows/01-project/01-2-update-filter.test.js
*/
'use strict';

// Flow 01 nhóm 2 — update (full replace), batch, filter GET, soft delete/restore. Case #3–#8.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { RUN_ID } = require('../../_lib/config');
const { ok, assertSameIds } = require('../../_lib/verify');

let a;

before(async () => { a = await dm.newSession('01-2'); });
after(async () => { await a?.cleanup(); });

test('Flow01#3: update name/description/status/ngày -> GET trả giá trị mới, ngày YYYY-MM-DD', async () => {
  const p = await a.createProject();
  ok(await a.upsertProjects([{
    id: p.id, name: `APITEST ${RUN_ID} after`, description: 'mô tả mới', status: 'paused',
    startDate: '2026-10-01', endDate: '2026-10-31', workspaceId: p.workspaceId,
  }]));
  const [got] = ok(await a.getProjectRes(p.id));
  assert.equal(got.name, `APITEST ${RUN_ID} after`);
  assert.equal(got.description, 'mô tả mới');
  assert.equal(got.status, 'paused');
  assert.equal(got.startDate, '2026-10-01');
  assert.equal(got.endDate, '2026-10-31');
});

test('Flow01#4: batch 3 project trong 1 request -> trả đủ 3', async () => {
  const data = ok(await a.upsertProjects([1, 2, 3].map((i) => ({ id: 0, name: `APITEST ${RUN_ID} batch${i}` }))));
  assert.equal(data.length, 3);
  assert.ok(data.every((p) => p.id > 0 && p.userId === a.id));
});

test('Flow01#5: filter status CSV (#5.1), ids (#5.2), searchText name/description (#5.3), mới tạo đứng trước (#5.4)', async () => {
  const p1 = await a.createProject({ status: 'active' });
  const p2 = await a.createProject({ status: 'paused' });
  const p3 = await a.createProject({ status: 'completed' });
  const ids = `${p1.id},${p2.id},${p3.id}`;

  assertSameIds(ok(await a.getProjects(`?ids=${ids}&status=active,paused`)).map((p) => p.id), [p1.id, p2.id], '#5.1');
  assert.deepEqual(ok(await a.getProjects(`?ids=${ids}`)).map((p) => p.id), [p3.id, p2.id, p1.id], '#5.2/#5.4');

  const token = `zq${RUN_ID}`;
  const byName = await a.createProject({ name: `APITEST ${token} name` });
  const byDesc = await a.createProject({ description: `có ${token} trong mô tả` });
  assertSameIds(ok(await a.getProjects(`?searchText=${token}`)).map((p) => p.id), [byName.id, byDesc.id], '#5.3');
});

test('Flow01#6/#8: GET /api/project/{id} trả đúng 1; status open/planned (0095) lưu được', async () => {
  for (const status of ['open', 'planned']) {
    const p = await a.createProject({ status });
    const data = ok(await a.getProjectRes(p.id));
    assert.equal(data.length, 1);
    assert.equal(data[0].status, status);
  }
});

test('Flow01#7: soft delete (#7.1) rồi restore (#7.2) qua upsert deletedAt', async () => {
  const p = await a.createProject();
  const base = { id: p.id, name: p.name, status: p.status, workspaceId: p.workspaceId };
  ok(await a.upsertProjects([{ ...base, deletedAt: new Date().toISOString() }]));
  assert.equal(ok(await a.getProjects(`?ids=${p.id}&deletedAt=null`)).length, 0, '#7.1 ẩn khỏi deletedAt=null');
  assert.equal(ok(await a.getProjects(`?ids=${p.id}&deletedAt=notNull`)).length, 1, '#7.1 thấy ở deletedAt=notNull');
  ok(await a.upsertProjects([{ ...base, deletedAt: null }]));
  assert.equal(ok(await a.getProjects(`?ids=${p.id}&deletedAt=null`)).length, 1, '#7.2 restore');
});
