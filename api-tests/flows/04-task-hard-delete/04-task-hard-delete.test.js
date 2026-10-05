/*
**Fullpath:** /api-tests/flows/04-task-hard-delete/04-task-hard-delete.test.js
*/
'use strict';

// Flow 04 — DELETE /api/task (hard delete). Xem testcase.md cùng folder.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const { RUN_ID, createUser, ok } = require('../../_lib/api');

const MISSING_ID = 2147483000;

let a;
let project;

before(async () => {
  a = await createUser('t04');
  project = await a.createProject({ name: `APITEST ${RUN_ID} hard-delete` });
});

after(async () => {
  await a?.cleanup();
});

test('Flow04#1: xoá task có comment + task con -> task mất hẳn (#1), comment mất (#1.1), con còn và parent=null (#1.2)', async () => {
  const parent = await a.createTask(project.id);
  const child = await a.createTask(project.id, { parentTaskId: parent.id });
  ok(await a.upsertComment({ taskId: parent.id, content: 'comment sẽ bị xoá' }), 'comment');

  const res = await a.hardDeleteTasks([parent.id]);
  ok(res, 'hard delete');

  assert.equal(ok(await a.getTasks(`?ids=${parent.id}`)).length, 0, 'task phải mất hẳn, không phải soft delete');
  const comments = await a.getComments(parent.id);
  assert.equal(comments.body?.status, 404, '#1.1 task không còn -> GET comment 404');

  const gotChild = await a.getTask(child.id);
  assert.ok(gotChild, '#1.2 task con không được bị xoá theo');
  assert.equal(gotChild.parentTaskId, null, '#1.2 parentTaskId của con phải về null');
});

test('Flow04#2: xoá nhiều task 1 lần', async () => {
  const t1 = await a.createTask(project.id);
  const t2 = await a.createTask(project.id);
  const res = await a.hardDeleteTasks([t1.id, t2.id]);
  ok(res);
  assert.match(res.body.message, /Permanently deleted 2 task/);
  assert.equal(ok(await a.getTasks(`?ids=${t1.id},${t2.id}`)).length, 0);
});

test('Flow04#3: ids rỗng -> HTTP 400', async () => {
  const res = await a.hardDeleteTasks([]);
  assert.equal(res.http, 400);
});

test('Flow04#4: > 200 id -> HTTP 400', async () => {
  const res = await a.hardDeleteTasks(Array.from({ length: 201 }, (_, i) => MISSING_ID - i));
  assert.equal(res.http, 400);
});

test('Flow04#5: lẫn 1 id không tồn tại -> 404, task hợp lệ trong lô vẫn còn', async () => {
  const t = await a.createTask(project.id, { title: `APITEST ${RUN_ID} survive` });
  const res = await a.hardDeleteTasks([t.id, MISSING_ID]);
  assert.equal(res.body?.success, false);
  assert.equal(res.body?.status, 404);
  assert.ok(await a.getTask(t.id), 'task hợp lệ không được bị xoá');
});
