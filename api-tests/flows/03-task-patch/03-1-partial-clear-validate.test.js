/*
**Fullpath:** /api-tests/flows/03-task-patch/03-1-partial-clear-validate.test.js
*/
'use strict';

// Flow 03 nhóm 1 — PATCH chỉ ghi field gửi lên, clearFields, validate. Case #1–#4, #10.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { RUN_ID, MISSING_ID } = require('../../_lib/config');
const { ok, assertFail } = require('../../_lib/verify');

let a;
let project;

before(async () => {
  a = await dm.newSession('03-1');
  project = await a.createProject();
});
after(async () => { await a?.cleanup(); });

test('Flow03#1: patch chỉ status -> các field khác giữ nguyên', async () => {
  const fields = {
    title: `APITEST ${RUN_ID} keep`, description: '<p>giữ</p>', priority: 'high',
    checklistJson: JSON.stringify({ items: [{ id: 'c1', text: 'a', checked: false }] }),
    startDate: '2026-10-02', endDate: '2026-10-03',
  };
  const t = await a.createTask(project.id, fields);
  ok(await a.patchTask(t.id, { status: 'in_progress' }));
  const got = await a.getTask(t.id);
  assert.equal(got.status, 'in_progress');
  for (const [k, v] of Object.entries(fields)) assert.equal(got[k], v, k);
});

test('Flow03#2/#10: patch nhiều field cùng lúc; alias note ghi vào description', async () => {
  const t = await a.createTask(project.id);
  const body = {
    title: `APITEST ${RUN_ID} multi`, priority: 'medium', type: 'repeat', taskType: 'work', orderIndex: 7,
    isMilestone: true, startDate: '2026-10-10', endDate: '2026-10-12',
  };
  ok(await a.patchTask(t.id, { ...body, note: '<p>qua note</p>' }));
  const got = await a.getTask(t.id);
  for (const [k, v] of Object.entries(body)) assert.equal(got[k], v, k);
  assert.equal(got.description, '<p>qua note</p>', '#10');
});

test('Flow03#3.1: clearFields description/startDate/endDate/parentTaskId/checklistJson -> null', async () => {
  const parent = await a.createTask(project.id);
  const t = await a.createTask(project.id, {
    parentTaskId: parent.id, description: '<p>x</p>', startDate: '2026-10-02', endDate: '2026-10-03', checklistJson: '{"items":[]}',
  });
  const cleared = ['description', 'startDate', 'endDate', 'parentTaskId', 'checklistJson'];
  ok(await a.patchTask(t.id, { clearFields: cleared }));
  const got = await a.getTask(t.id);
  for (const k of cleared) assert.equal(got[k], null, k);
});

test('Flow03#3.2/#4.1/#4.2/#4.3: clear field cấm, title rỗng, status rỗng, cha = chính nó -> body.status 400, không ghi gì', async () => {
  const t = await a.createTask(project.id, { title: `APITEST ${RUN_ID} guard` });
  assertFail(await a.patchTask(t.id, { clearFields: ['title'] }), 400, /Cannot clear field\(s\): title/, '#3.2');
  assertFail(await a.patchTask(t.id, { title: '   ' }), 400, /Title cannot be empty/, '#4.1');
  assertFail(await a.patchTask(t.id, { status: '' }), 400, /Status cannot be empty/, '#4.2');
  assertFail(await a.patchTask(t.id, { parentTaskId: t.id }), 400, /cannot be its own parent/, '#4.3');
  const got = await a.getTask(t.id);
  assert.equal(got.title, `APITEST ${RUN_ID} guard`);
  assert.equal(got.parentTaskId, null);
});

test('Flow03#4.4/#4.5: task không tồn tại -> HTTP 404; id = 0 -> HTTP 400', async () => {
  assert.equal((await a.patchTask(MISSING_ID, { status: 'open' })).http, 404, '#4.4');
  assert.equal((await a.patchTask(0, { status: 'open' })).http, 400, '#4.5');
});
