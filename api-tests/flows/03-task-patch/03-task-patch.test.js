/*
**Fullpath:** /api-tests/flows/03-task-patch/03-task-patch.test.js
*/
'use strict';

// Flow 03 — PATCH /api/task/{id}. Xem testcase.md cùng folder.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const { RUN_ID, createUser, ok } = require('../../_lib/api');

const MISSING_ID = 2147483000;

let a;
let project;

before(async () => {
  a = await createUser('t03');
  project = await a.createProject({ name: `APITEST ${RUN_ID} patch`, startDate: '2026-10-01', endDate: '2026-10-31' });
});

after(async () => {
  await a?.cleanup();
});

test('Flow03#1: patch chỉ status -> các field khác giữ nguyên', async () => {
  const checklistJson = JSON.stringify({ items: [{ id: 'c1', text: 'a', checked: false }] });
  const t = await a.createTask(project.id, {
    title: `APITEST ${RUN_ID} keep`, description: '<p>giữ</p>', checklistJson, startDate: '2026-10-02', endDate: '2026-10-03', priority: 'high',
  });
  const [patched] = ok(await a.patchTask(t.id, { status: 'in_progress' }), 'patch');
  assert.equal(patched.status, 'in_progress');

  const got = await a.getTask(t.id);
  assert.equal(got.status, 'in_progress');
  assert.equal(got.title, `APITEST ${RUN_ID} keep`);
  assert.equal(got.description, '<p>giữ</p>');
  assert.equal(got.checklistJson, checklistJson);
  assert.equal(got.startDate, '2026-10-02');
  assert.equal(got.endDate, '2026-10-03');
  assert.equal(got.priority, 'high');
});

test('Flow03#2: patch nhiều field cùng lúc', async () => {
  const t = await a.createTask(project.id);
  ok(await a.patchTask(t.id, {
    title: `APITEST ${RUN_ID} multi`, priority: 'medium', type: 'repeat', taskType: 'work', orderIndex: 7,
    isMilestone: true, startDate: '2026-10-10', endDate: '2026-10-12',
  }));
  const got = await a.getTask(t.id);
  assert.equal(got.title, `APITEST ${RUN_ID} multi`);
  assert.equal(got.priority, 'medium');
  assert.equal(got.type, 'repeat');
  assert.equal(got.taskType, 'work');
  assert.equal(got.orderIndex, 7);
  assert.equal(got.isMilestone, true);
  assert.equal(got.startDate, '2026-10-10');
  assert.equal(got.endDate, '2026-10-12');
});

test('Flow03#3.1: clearFields description/startDate/endDate/parentTaskId/checklistJson -> null', async () => {
  const parent = await a.createTask(project.id);
  const t = await a.createTask(project.id, {
    parentTaskId: parent.id, description: '<p>x</p>', startDate: '2026-10-02', endDate: '2026-10-03', checklistJson: '{"items":[]}',
  });
  ok(await a.patchTask(t.id, { clearFields: ['description', 'startDate', 'endDate', 'parentTaskId', 'checklistJson'] }));
  const got = await a.getTask(t.id);
  assert.equal(got.description, null);
  assert.equal(got.startDate, null);
  assert.equal(got.endDate, null);
  assert.equal(got.parentTaskId, null);
  assert.equal(got.checklistJson, null);
});

test('Flow03#3.2: clearFields field không cho phép (title) -> 400', async () => {
  const t = await a.createTask(project.id);
  const res = await a.patchTask(t.id, { clearFields: ['title'] });
  assert.equal(res.body?.success, false);
  assert.equal(res.body?.status, 400);
  assert.match(res.body?.message ?? '', /Cannot clear field\(s\): title/);
});

test('Flow03#4.1/#4.2/#4.3: title rỗng, status rỗng, parent = chính nó -> 400', async () => {
  const t = await a.createTask(project.id);
  for (const [body, msg] of [
    [{ title: '   ' }, /Title cannot be empty/],
    [{ status: '' }, /Status cannot be empty/],
    [{ parentTaskId: t.id }, /cannot be its own parent/],
  ]) {
    const res = await a.patchTask(t.id, body);
    assert.equal(res.body?.status, 400, `body ${JSON.stringify(body)} phải trả 400`);
    assert.match(res.body?.message ?? '', msg);
  }
  const got = await a.getTask(t.id);
  assert.notEqual(got.title.trim(), '', 'title không được bị ghi rỗng');
});

test('Flow03#4.4: patch task không tồn tại -> HTTP 404', async () => {
  const res = await a.patchTask(MISSING_ID, { status: 'open' });
  assert.equal(res.http, 404);
});

test('Flow03#4.5: patch id=0 -> HTTP 400', async () => {
  const res = await a.patchTask(0, { status: 'open' });
  assert.equal(res.http, 400);
});

test('Flow03#5: chuyển task sang project khác -> response kèm ngày giới hạn của project mới', async () => {
  const p2 = await a.createProject({ name: `APITEST ${RUN_ID} patch-p2`, startDate: '2026-11-01', endDate: '2026-11-30' });
  const t = await a.createTask(project.id);
  const [patched] = ok(await a.patchTask(t.id, { projectId: p2.id }));
  assert.equal(patched.projectId, p2.id);
  assert.equal(patched.projectStartDate, '2026-11-01');
  assert.equal(patched.projectEndDate, '2026-11-30');
});

test('Flow03#6: gán parentTaskId -> response kèm ngày của cha', async () => {
  const parent = await a.createTask(project.id, { startDate: '2026-10-06', endDate: '2026-10-09' });
  const t = await a.createTask(project.id);
  const [patched] = ok(await a.patchTask(t.id, { parentTaskId: parent.id }));
  assert.equal(patched.parentTaskId, parent.id);
  assert.equal(patched.parentStartDate, '2026-10-06');
  assert.equal(patched.parentEndDate, '2026-10-09');
});

test('Flow03#7: ngày ISO có offset Z -> quy về ngày theo timezone user (17:30Z = 00:30 +07 ngày hôm sau)', async () => {
  const t = await a.createTask(project.id);
  ok(await a.patchTask(t.id, { startDate: '2026-10-04T17:30:00Z', endDate: '2026-10-04T10:00:00Z' }));
  const got = await a.getTask(t.id);
  assert.equal(got.startDate, '2026-10-05');
  assert.equal(got.endDate, '2026-10-04');
});

test('Flow03#8: (1468) completed khi checklist chưa tick hết -> cho phép', async () => {
  const checklistJson = JSON.stringify({ items: [{ id: 'c1', text: 'a', checked: true }, { id: 'c2', text: 'b', checked: false }] });
  const t = await a.createTask(project.id, { checklistJson });
  ok(await a.patchTask(t.id, { status: 'completed' }));
  const got = await a.getTask(t.id);
  assert.equal(got.status, 'completed');
  assert.equal(got.checklistJson, checklistJson, 'checklist không bị đụng');
});

test('Flow03#9: (1468) patch processJson tick hết step -> status không tự đổi', async () => {
  const t = await a.createTask(project.id, { status: 'open', processJson: JSON.stringify({ items: [{ id: 'p1', text: 's', checked: false }] }) });
  ok(await a.patchTask(t.id, { processJson: JSON.stringify({ items: [{ id: 'p1', text: 's', checked: true }] }) }));
  const got = await a.getTask(t.id);
  assert.equal(got.status, 'open');
});

test('Flow03#10: alias note trong PATCH -> ghi vào description', async () => {
  const t = await a.createTask(project.id);
  ok(await a.patchTask(t.id, { note: '<p>qua note</p>' }));
  const got = await a.getTask(t.id);
  assert.equal(got.description, '<p>qua note</p>');
});
