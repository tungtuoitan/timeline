/*
**Fullpath:** /api-tests/flows/03-task-patch/03-2-move-dates-status.test.js
*/
'use strict';

// Flow 03 nhóm 2 — đổi project/parent (response kèm ngày giới hạn mới), ngày ISO cũ, rule 1468.
// Case #5–#9.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { ok } = require('../../_lib/verify');

let a;
let project;

before(async () => {
  a = await dm.newSession('03-2');
  project = await a.createProject({ startDate: '2026-10-01', endDate: '2026-10-31' });
});
after(async () => { await a?.cleanup(); });

test('Flow03#5/#6: chuyển project / gán cha -> response kèm ngày giới hạn của project mới / cha', async () => {
  const p2 = await a.createProject({ startDate: '2026-11-01', endDate: '2026-11-30' });
  const t = await a.createTask(project.id);
  const [moved] = ok(await a.patchTask(t.id, { projectId: p2.id }));
  assert.equal(moved.projectId, p2.id);
  assert.equal(moved.projectStartDate, '2026-11-01', '#5');
  assert.equal(moved.projectEndDate, '2026-11-30', '#5');

  const parent = await a.createTask(p2.id, { startDate: '2026-11-06', endDate: '2026-11-09' });
  const [child] = ok(await a.patchTask(t.id, { parentTaskId: parent.id }));
  assert.equal(child.parentTaskId, parent.id);
  assert.equal(child.parentStartDate, '2026-11-06', '#6');
  assert.equal(child.parentEndDate, '2026-11-09', '#6');
});

test('Flow03#7: ngày ISO có Z -> quy về ngày theo timezone user (17:30Z = 00:30 +07 hôm sau)', async () => {
  const t = await a.createTask(project.id);
  ok(await a.patchTask(t.id, { startDate: '2026-10-04T17:30:00Z', endDate: '2026-10-04T10:00:00Z' }));
  const got = await a.getTask(t.id);
  assert.equal(got.startDate, '2026-10-05');
  assert.equal(got.endDate, '2026-10-04');
});

test('Flow03#8/#9 (1468): completed khi checklist chưa tick hết được phép; tick hết process không tự đổi status', async () => {
  const checklistJson = JSON.stringify({ items: [{ id: 'c1', text: 'a', checked: true }, { id: 'c2', text: 'b', checked: false }] });
  const t1 = await a.createTask(project.id, { checklistJson });
  ok(await a.patchTask(t1.id, { status: 'completed' }));
  const got1 = await a.getTask(t1.id);
  assert.equal(got1.status, 'completed', '#8');
  assert.equal(got1.checklistJson, checklistJson, '#8 checklist không bị đụng');

  const t2 = await a.createTask(project.id, { status: 'open', processJson: JSON.stringify({ items: [{ id: 'p1', text: 's', checked: false }] }) });
  ok(await a.patchTask(t2.id, { processJson: JSON.stringify({ items: [{ id: 'p1', text: 's', checked: true }] }) }));
  assert.equal((await a.getTask(t2.id)).status, 'open', '#9');
});
