/*
**Fullpath:** /api-tests/flows/02-task-upsert/02-3-soft-delete-filter.test.js
*/
'use strict';

// Flow 02 nhóm 3 — soft delete/restore và filter GET. Case #7–#9.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { RUN_ID, MISSING_ID } = require('../../_lib/config');
const { ok, assertSameIds } = require('../../_lib/verify');

let a;

before(async () => { a = await dm.newSession('02-3'); });
after(async () => { await a?.cleanup(); });

test('Flow02#7: soft delete rồi restore qua upsert deletedAt', async () => {
  const p = await a.createProject();
  const t = await a.createTask(p.id);
  const base = { id: t.id, projectId: p.id, title: t.title };
  ok(await a.upsertTasks([{ ...base, deletedAt: new Date().toISOString() }]));
  assert.equal(ok(await a.getTasks(`?ids=${t.id}&deletedAt=null`)).length, 0);
  assert.equal(ok(await a.getTasks(`?ids=${t.id}&deletedAt=notNull`)).length, 1);
  ok(await a.upsertTasks([{ ...base, deletedAt: null }]));
  assert.equal(ok(await a.getTasks(`?ids=${t.id}&deletedAt=null`)).length, 1);
});

test('Flow02#8: filter status khớp chính xác (#8.1), priority (#8.2), type (#8.3), searchText (#8.4), projectIds (#8.5), sắp orderIndex (#8.6)', async () => {
  const p = await a.createProject();
  const other = await a.createProject();
  const token = `zq${RUN_ID}`;
  const tOpen = await a.createTask(p.id, { status: 'open', priority: 'high', orderIndex: 3 });
  const tReopened = await a.createTask(p.id, { status: 'reopened', priority: 'low', orderIndex: 1, title: `APITEST ${token}` });
  const tMilestone = await a.createTask(p.id, { status: 'completed', priority: 'medium', type: 'milestone', orderIndex: 2, description: `<p>${token}</p>` });
  const tOther = await a.createTask(other.id, { status: 'open' });
  const ids = async (q) => ok(await a.getTasks(q)).map((t) => t.id);

  assert.deepEqual(await ids(`?projectIds=${p.id}&status=open`), [tOpen.id], '#8.1 "open" không khớp "reopened"');
  assertSameIds(await ids(`?projectIds=${p.id}&status=open,completed`), [tOpen.id, tMilestone.id], '#8.1 CSV');
  assertSameIds(await ids(`?projectIds=${p.id}&priority=high,medium`), [tOpen.id, tMilestone.id], '#8.2');
  assert.deepEqual(await ids(`?projectIds=${p.id}&type=milestone`), [tMilestone.id], '#8.3');
  assertSameIds(await ids(`?searchText=${token}`), [tReopened.id, tMilestone.id], '#8.4');
  assertSameIds(await ids(`?projectIds=${p.id},${other.id}&status=open`), [tOpen.id, tOther.id], '#8.5');
  assert.deepEqual(await ids(`?projectIds=${p.id}`), [tReopened.id, tMilestone.id, tOpen.id], '#8.6');
});

test('Flow02#9: GET /api/task/{id} không tồn tại -> success, data rỗng', async () => {
  const res = await a.call('GET', `/api/task/${MISSING_ID}`);
  assert.equal(res.body?.success, true);
  assert.equal(res.body?.data?.length, 0);
});
