/*
**Fullpath:** /api-tests/flows/02-task-upsert/02-task-upsert.test.js
*/
'use strict';

// Flow 02 — Task upsert + query. Xem testcase.md cùng folder.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const { RUN_ID, createUser, ok } = require('../../_lib/api');

const MISSING_ID = 2147483000; // id chắc chắn không tồn tại
const ISO_WITH_OFFSET = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?([+-]\d{2}:\d{2}|Z)$/;

let a;
let project;

before(async () => {
  a = await createUser('t02');
  project = await a.createProject({ name: `APITEST ${RUN_ID} tasks`, startDate: '2026-10-01', endDate: '2026-12-31' });
});

after(async () => {
  await a?.cleanup();
});

test('Flow02#1: tạo task tối thiểu -> default type/taskType/status/priority/isMilestone; createdAt có offset (#1.1)', async () => {
  const t = await a.createTask(project.id);
  assert.ok(t.id > 0);
  assert.equal(t.type, 'task');
  assert.equal(t.taskType, 'personal');
  assert.equal(t.status, 'open');
  assert.equal(t.priority, 'low');
  assert.equal(t.isMilestone, false);
  assert.match(t.createdAt, ISO_WITH_OFFSET, `createdAt phải có offset, thực tế: ${t.createdAt}`);
  assert.match(t.updatedAt, ISO_WITH_OFFSET);
});

test('Flow02#2.1: thiếu title -> HTTP 400', async () => {
  const res = await a.call('POST', '/api/task', [{ id: 0, projectId: project.id }]);
  assert.equal(res.http, 400);
});

test('Flow02#2.2: title > 500 ký tự -> HTTP 400', async () => {
  const res = await a.call('POST', '/api/task', [{ id: 0, projectId: project.id, title: 'x'.repeat(501) }]);
  assert.equal(res.http, 400);
});

test('Flow02#2.3: projectId không tồn tại -> 404 "Project not found or access denied"', async () => {
  const res = await a.upsertTasks([{ id: 0, projectId: MISSING_ID, title: 'x' }]);
  assert.equal(res.body?.success, false);
  assert.equal(res.body?.status, 404);
  assert.match(res.body?.message ?? '', /Project not found/);
});

test('Flow02#2.4: tạo mới kèm deletedAt -> 400', async () => {
  const res = await a.upsertTasks([{ id: 0, projectId: project.id, title: 'x', deletedAt: new Date().toISOString() }]);
  assert.equal(res.body?.success, false);
  assert.equal(res.body?.status, 400);
});

test('Flow02#2.5: update id không tồn tại -> 404 "Task not found or access denied"', async () => {
  const res = await a.upsertTasks([{ id: MISSING_ID, projectId: project.id, title: 'x' }]);
  assert.equal(res.body?.status, 404);
  assert.match(res.body?.message ?? '', /Task not found/);
});

test('Flow02#3: batch có 1 phần tử lỗi -> cả lô bị từ chối, phần tử hợp lệ không được tạo', async () => {
  const title = `APITEST ${RUN_ID} atomic-${Date.now()}`;
  const res = await a.upsertTasks([
    { id: 0, projectId: project.id, title },
    { id: 0, projectId: MISSING_ID, title },
  ]);
  assert.equal(res.body?.success, false);
  const found = ok(await a.getTasks(`?projectIds=${project.id}&searchText=${encodeURIComponent(title)}`));
  assert.equal(found.length, 0, 'không được có task nào được tạo từ lô lỗi');
});

test('Flow02#4: update full ghi đè field; description null giữ nguyên (#4.1), "" xoá (#4.2), alias note (#4.3)', async () => {
  const t = await a.createTask(project.id, { description: '<p>mô tả gốc</p>' });
  const base = { id: t.id, projectId: project.id, title: `APITEST ${RUN_ID} upd`, status: 'in_progress', priority: 'high' };

  ok(await a.upsertTasks([base]), 'update không gửi description');
  let got = await a.getTask(t.id);
  assert.equal(got.title, base.title);
  assert.equal(got.status, 'in_progress');
  assert.equal(got.priority, 'high');
  assert.equal(got.description, '<p>mô tả gốc</p>', '#4.1 description phải giữ nguyên khi không gửi');

  ok(await a.upsertTasks([{ ...base, note: '<p>từ note</p>' }]), 'update qua note');
  got = await a.getTask(t.id);
  assert.equal(got.description, '<p>từ note</p>', '#4.3 alias note phải ghi vào description');

  ok(await a.upsertTasks([{ ...base, description: '' }]), 'update description rỗng');
  got = await a.getTask(t.id);
  assert.equal(got.description, '', '#4.2 description "" phải xoá');
});

test('Flow02#5: checklistJson/processJson/customTabsJson + isMilestone round-trip', async () => {
  const checklistJson = JSON.stringify({ items: [{ id: 'c1', text: 'bước 1', checked: false }] });
  const processJson = JSON.stringify({ items: [{ id: 'p1', text: 'step', checked: true }] });
  const customTabsJson = JSON.stringify([{ name: 'Tab A', version: 1, content: '<p>x</p>' }]);
  const t = await a.createTask(project.id, { checklistJson, processJson, customTabsJson, isMilestone: true });
  const got = await a.getTask(t.id);
  assert.equal(got.checklistJson, checklistJson);
  assert.equal(got.processJson, processJson);
  assert.equal(got.customTabsJson, customTabsJson);
  assert.equal(got.isMilestone, true);
});

test('Flow02#6: task con -> GET trả parentStartDate/EndDate + projectStartDate/EndDate', async () => {
  const parent = await a.createTask(project.id, { startDate: '2026-10-05', endDate: '2026-10-20' });
  const child = await a.createTask(project.id, { parentTaskId: parent.id });
  const got = await a.getTask(child.id);
  assert.equal(got.parentTaskId, parent.id);
  assert.equal(got.parentStartDate, '2026-10-05');
  assert.equal(got.parentEndDate, '2026-10-20');
  assert.equal(got.projectStartDate, '2026-10-01');
  assert.equal(got.projectEndDate, '2026-12-31');
});

test('Flow02#7: soft delete rồi restore qua upsert deletedAt', async () => {
  const t = await a.createTask(project.id);
  const base = { id: t.id, projectId: project.id, title: t.title };
  ok(await a.upsertTasks([{ ...base, deletedAt: new Date().toISOString() }]), 'soft delete');
  assert.equal(ok(await a.getTasks(`?ids=${t.id}&deletedAt=null`)).length, 0);
  assert.equal(ok(await a.getTasks(`?ids=${t.id}&deletedAt=notNull`)).length, 1);
  ok(await a.upsertTasks([{ ...base, deletedAt: null }]), 'restore');
  assert.equal(ok(await a.getTasks(`?ids=${t.id}&deletedAt=null`)).length, 1);
});

test('Flow02#8: filter status exact (#8.1), priority (#8.2), type (#8.3), searchText (#8.4), projectIds (#8.5), thứ tự orderIndex (#8.6)', async () => {
  const p = await a.createProject({ name: `APITEST ${RUN_ID} filter` });
  const other = await a.createProject({ name: `APITEST ${RUN_ID} filter-other` });
  const token = `zq${RUN_ID}`;
  const tOpen = await a.createTask(p.id, { status: 'open', priority: 'high', orderIndex: 3 });
  const tReopened = await a.createTask(p.id, { status: 'reopened', priority: 'low', orderIndex: 1, title: `APITEST ${token} title` });
  const tMilestone = await a.createTask(p.id, { status: 'completed', priority: 'medium', type: 'milestone', orderIndex: 2, description: `<p>${token}</p>` });
  const tOther = await a.createTask(other.id, { status: 'open' });

  const ids = (data) => data.map((t) => t.id);
  assert.deepEqual(ids(ok(await a.getTasks(`?projectIds=${p.id}&status=open`))), [tOpen.id], '#8.1 status=open không được trả reopened');
  assert.deepEqual(ids(ok(await a.getTasks(`?projectIds=${p.id}&status=open,completed`))).sort(), [tOpen.id, tMilestone.id].sort());
  assert.deepEqual(ids(ok(await a.getTasks(`?projectIds=${p.id}&priority=high,medium`))).sort(), [tOpen.id, tMilestone.id].sort(), '#8.2');
  assert.deepEqual(ids(ok(await a.getTasks(`?projectIds=${p.id}&type=milestone`))), [tMilestone.id], '#8.3');
  assert.deepEqual(ids(ok(await a.getTasks(`?searchText=${token}`))).sort(), [tReopened.id, tMilestone.id].sort(), '#8.4');
  assert.deepEqual(ids(ok(await a.getTasks(`?projectIds=${p.id},${other.id}&status=open`))).sort(), [tOpen.id, tOther.id].sort(), '#8.5');
  assert.deepEqual(ids(ok(await a.getTasks(`?projectIds=${p.id}`))), [tReopened.id, tMilestone.id, tOpen.id], '#8.6 sắp theo orderIndex');
});

test('Flow02#9: GET /api/task/{id} không tồn tại -> success, data rỗng', async () => {
  const res = await a.call('GET', `/api/task/${MISSING_ID}`);
  assert.equal(res.body?.success, true);
  assert.equal(res.body?.data?.length, 0);
});

test('Flow02#10: startDate/endDate YYYY-MM-DD lưu & trả nguyên (không lệch múi giờ)', async () => {
  const t = await a.createTask(project.id, { startDate: '2026-10-04', endDate: '2026-10-05' });
  const got = await a.getTask(t.id);
  assert.equal(got.startDate, '2026-10-04');
  assert.equal(got.endDate, '2026-10-05');
});
