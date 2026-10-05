/*
**Fullpath:** /api-tests/flows/05-task-comment/05-2-edit-reply-delete.test.js
*/
'use strict';

// Flow 05 nhóm 2 — sửa, reply, xoá (lan xuống reply). Case #6–#8.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { MISSING_ID } = require('../../_lib/config');
const { ok, assertFail, assertDenied } = require('../../_lib/verify');

let a;
let project;

before(async () => {
  a = await dm.newSession('05-2');
  project = await a.createProject();
});
after(async () => { await a?.cleanup(); });

test('Flow05#6.1/#6.2: sửa content giữ nguyên type + occurredAt; sửa với taskId lệch -> 404', async () => {
  const t = await a.createTask(project.id);
  const c = await a.addComment(t.id, { content: 'bản đầu', type: 'decision', occurredAt: '2026-09-30T03:00:00Z' });
  const [edited] = ok(await a.upsertComment({ id: c.id, taskId: t.id, content: 'bản sửa' }));
  assert.equal(edited.content, 'bản sửa');
  assert.equal(edited.type, 'decision', '#6.1');
  assert.equal(Date.parse(edited.occurredAt), Date.parse('2026-09-30T03:00:00Z'), '#6.1');

  const other = await a.createTask(project.id);
  assertDenied(await a.upsertComment({ id: c.id, taskId: other.id, content: 'lệch task' }), '#6.2');
});

test('Flow05#7/#8/#6.3: reply cùng task OK, cha khác task 404; xoá cha ẩn luôn reply; sửa comment đã xoá 404', async () => {
  const t = await a.createTask(project.id);
  const elsewhere = await a.createTask(project.id);
  const parent = await a.addComment(t.id, { content: 'cha' });
  const reply = await a.addComment(t.id, { content: 'con', parentCommentId: parent.id });
  assert.equal(reply.parentCommentId, parent.id, '#7.1');
  assertDenied(await a.upsertComment({ taskId: elsewhere.id, content: 'reply lệch task', parentCommentId: parent.id }), '#7.2');

  ok(await a.deleteComment(parent.id));
  assert.equal(ok(await a.getComments(t.id)).length, 0, '#8 cha + reply đều ẩn');
  assertFail(await a.upsertComment({ id: parent.id, taskId: t.id, content: 'sửa cái đã xoá' }), 404, null, '#6.3');
});

test('Flow05#8.1: xoá comment không tồn tại -> 404', async () => {
  assertFail(await a.deleteComment(MISSING_ID), 404);
});
