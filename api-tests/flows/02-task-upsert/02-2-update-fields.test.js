/*
**Fullpath:** /api-tests/flows/02-task-upsert/02-2-update-fields.test.js
*/
'use strict';

// Flow 02 nhóm 2 — update full replace + quy tắc riêng của description, JSON field, task con, ngày.
// Case #4–#6, #10.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { RUN_ID } = require('../../_lib/config');
const { ok } = require('../../_lib/verify');

let a;
let project;

before(async () => {
  a = await dm.newSession('02-2');
  project = await a.createProject({ startDate: '2026-10-01', endDate: '2026-12-31' });
});
after(async () => { await a?.cleanup(); });

test('Flow02#4: update ghi đè field; description không gửi = giữ (#4.1), alias note (#4.3), "" = xoá (#4.2)', async () => {
  const t = await a.createTask(project.id, { description: '<p>gốc</p>' });
  const base = { id: t.id, projectId: project.id, title: `APITEST ${RUN_ID} upd`, status: 'in_progress', priority: 'high' };

  ok(await a.upsertTasks([base]));
  let got = await a.getTask(t.id);
  assert.equal(got.title, base.title);
  assert.equal(got.status, 'in_progress');
  assert.equal(got.priority, 'high');
  assert.equal(got.description, '<p>gốc</p>', '#4.1');

  ok(await a.upsertTasks([{ ...base, note: '<p>từ note</p>' }]));
  assert.equal((await a.getTask(t.id)).description, '<p>từ note</p>', '#4.3');

  ok(await a.upsertTasks([{ ...base, description: '' }]));
  assert.equal((await a.getTask(t.id)).description, '', '#4.2');
});

test('Flow02#5: checklistJson/processJson/customTabsJson + isMilestone round-trip nguyên vẹn', async () => {
  const fields = {
    checklistJson: JSON.stringify({ items: [{ id: 'c1', text: 'bước 1', checked: false }] }),
    processJson: JSON.stringify({ items: [{ id: 'p1', text: 'step', checked: true }] }),
    customTabsJson: JSON.stringify([{ name: 'Tab A', version: 1, content: '<p>x</p>' }]),
    isMilestone: true,
  };
  const t = await a.createTask(project.id, fields);
  const got = await a.getTask(t.id);
  for (const [k, v] of Object.entries(fields)) assert.equal(got[k], v, k);
});

test('Flow02#6/#10: task con kèm ngày giới hạn của cha + project; ngày YYYY-MM-DD không lệch múi giờ', async () => {
  const parent = await a.createTask(project.id, { startDate: '2026-10-05', endDate: '2026-10-20' });
  const child = await a.createTask(project.id, { parentTaskId: parent.id, startDate: '2026-10-06', endDate: '2026-10-07' });
  const got = await a.getTask(child.id);
  assert.equal(got.parentTaskId, parent.id);
  assert.equal(got.parentStartDate, '2026-10-05');
  assert.equal(got.parentEndDate, '2026-10-20');
  assert.equal(got.projectStartDate, '2026-10-01');
  assert.equal(got.projectEndDate, '2026-12-31');
  assert.equal(got.startDate, '2026-10-06', '#10');
  assert.equal(got.endDate, '2026-10-07', '#10');
});
