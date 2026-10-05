/*
**Fullpath:** /api-tests/flows/02-task-upsert/02-1-create-validate.test.js
*/
'use strict';

// Flow 02 nhóm 1 — tạo task, validate, lô all-or-nothing. Case #1–#3.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { RUN_ID, MISSING_ID } = require('../../_lib/config');
const { ok, assertFail, assertDenied, assertHttp400, ISO_WITH_OFFSET } = require('../../_lib/verify');

let a;
let project;

before(async () => {
  a = await dm.newSession('02-1');
  project = await a.createProject();
});
after(async () => { await a?.cleanup(); });

test('Flow02#1: tạo task tối thiểu -> default type/taskType/status/priority/isMilestone; createdAt có offset (#1.1)', async () => {
  const t = await a.createTask(project.id);
  assert.ok(t.id > 0);
  assert.equal(t.type, 'task');
  assert.equal(t.taskType, 'personal');
  assert.equal(t.status, 'open');
  assert.equal(t.priority, 'low');
  assert.equal(t.isMilestone, false);
  assert.match(t.createdAt, ISO_WITH_OFFSET, '#1.1');
  assert.match(t.updatedAt, ISO_WITH_OFFSET, '#1.1');
});

test('Flow02#2.1/#2.2: thiếu title, title > 500 -> HTTP 400', async () => {
  assertHttp400(await a.call('POST', '/api/task', [{ id: 0, projectId: project.id }]), '#2.1');
  assertHttp400(await a.call('POST', '/api/task', [{ id: 0, projectId: project.id, title: 'x'.repeat(501) }]), '#2.2');
});

test('Flow02#2.3/#2.4/#2.5: project không tồn tại 404, deletedAt khi tạo 400, id không tồn tại 404', async () => {
  assertDenied(await a.upsertTasks([{ id: 0, projectId: MISSING_ID, title: 'x' }]), '#2.3');
  assertFail(await a.upsertTasks([{ id: 0, projectId: project.id, title: 'x', deletedAt: new Date().toISOString() }]), 400, /deletedAt/, '#2.4');
  assertDenied(await a.upsertTasks([{ id: MISSING_ID, projectId: project.id, title: 'x' }]), '#2.5');
});

test('Flow02#3: lô có 1 phần tử lỗi -> cả lô bị từ chối, phần tử hợp lệ cũng không được tạo', async () => {
  const title = `APITEST ${RUN_ID} atomic`;
  assertDenied(await a.upsertTasks([
    { id: 0, projectId: project.id, title },
    { id: 0, projectId: MISSING_ID, title },
  ]));
  const found = ok(await a.getTasks(`?projectIds=${project.id}&searchText=${encodeURIComponent(title)}`));
  assert.equal(found.length, 0);
});
