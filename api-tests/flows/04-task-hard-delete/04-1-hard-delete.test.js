/*
**Fullpath:** /api-tests/flows/04-task-hard-delete/04-1-hard-delete.test.js
*/
'use strict';

// Flow 04 — DELETE /api/task (xoá vĩnh viễn). Case #1–#5.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { MISSING_ID } = require('../../_lib/config');
const { ok, assertDenied, assertHttp400 } = require('../../_lib/verify');

let a;
let project;

before(async () => {
  a = await dm.newSession('04-1');
  project = await a.createProject();
});
after(async () => { await a?.cleanup(); });

test('Flow04#1: xoá task có comment + task con -> task mất hẳn, comment mất (#1.1), con còn với parent=null (#1.2)', async () => {
  const parent = await a.createTask(project.id);
  const child = await a.createTask(project.id, { parentTaskId: parent.id });
  await a.addComment(parent.id, { content: 'sẽ bị xoá' });

  ok(await a.hardDeleteTasks([parent.id]));
  assert.equal(ok(await a.getTasks(`?ids=${parent.id}`)).length, 0, 'không phải soft delete');
  assertDenied(await a.getComments(parent.id), '#1.1');
  const gotChild = await a.getTask(child.id);
  assert.ok(gotChild, '#1.2 task con còn');
  assert.equal(gotChild.parentTaskId, null, '#1.2');
});

test('Flow04#2: xoá nhiều task 1 lần', async () => {
  const t1 = await a.createTask(project.id);
  const t2 = await a.createTask(project.id);
  const res = await a.hardDeleteTasks([t1.id, t2.id]);
  ok(res);
  assert.match(res.body.message, /Permanently deleted 2 task/);
  assert.equal(ok(await a.getTasks(`?ids=${t1.id},${t2.id}`)).length, 0);
});

test('Flow04#3/#4: ids rỗng hoặc > 200 id -> HTTP 400', async () => {
  assertHttp400(await a.hardDeleteTasks([]), '#3');
  assertHttp400(await a.hardDeleteTasks(Array.from({ length: 201 }, (_, i) => MISSING_ID - i)), '#4');
});

test('Flow04#6: xoá task có comment kèm reply (kể cả reply của comment đã soft delete) -> thành công', async () => {
  const t = await a.createTask(project.id);
  const parent = await a.addComment(t.id, { content: 'cha' });
  await a.addComment(t.id, { content: 'con', parentCommentId: parent.id });
  const t2 = await a.createTask(project.id);
  const parent2 = await a.addComment(t2.id, { content: 'cha' });
  await a.addComment(t2.id, { content: 'con', parentCommentId: parent2.id });
  ok(await a.deleteComment(parent2.id));

  ok(await a.hardDeleteTasks([t.id, t2.id]), 'từng lỗi 500 FK_task_comment_parent');
  assert.equal(ok(await a.getTasks(`?ids=${t.id},${t2.id}`)).length, 0);
});

test('Flow04#5: lẫn 1 id không tồn tại -> 404, task hợp lệ trong lô vẫn còn', async () => {
  const t = await a.createTask(project.id);
  assertDenied(await a.hardDeleteTasks([t.id, MISSING_ID]));
  assert.ok(await a.getTask(t.id));
});
