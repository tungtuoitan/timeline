/*
**Fullpath:** /api-tests/flows/05-task-comment/05-1-create-type-filter.test.js
*/
'use strict';

// Flow 05 nhóm 1 — tạo comment, type, filter, occurredAt. Case #1–#5.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { MISSING_ID } = require('../../_lib/config');
const { ok, assertFail, assertHttp400, ISO_WITH_OFFSET } = require('../../_lib/verify');

let a;
let project;
let task;

before(async () => {
  a = await dm.newSession('05-1');
  project = await a.createProject();
  task = await a.createTask(project.id);
});
after(async () => { await a?.cleanup(); });

test('Flow05#1: tạo không gửi type -> type=comment, userId đúng, occurredAt null, createdAt có offset, message "Comment created" (#1.1)', async () => {
  const res = await a.upsertComment({ taskId: task.id, content: 'hello' });
  const [c] = ok(res);
  assert.equal(c.type, 'comment');
  assert.equal(c.userId, a.id);
  assert.equal(c.occurredAt ?? null, null);
  assert.match(c.createdAt, ISO_WITH_OFFSET);
  assert.equal(res.body.message, 'Comment created', '#1.1 (bug đã sửa ở 3bec94a: message từng luôn là "Comment updated")');
});

test('Flow05#2/#3: type devlog/decision/track lưu đúng; type lạ (#2.1), content rỗng/khoảng trắng (#3) bị từ chối', async () => {
  for (const type of ['devlog', 'decision', 'track']) {
    assert.equal((await a.addComment(task.id, { type })).type, type);
  }
  assertFail(await a.upsertComment({ taskId: task.id, content: 'x', type: 'foo' }), 400, /Unknown comment type/, '#2.1');
  // [Required] mặc định coi chuỗi toàn khoảng trắng là rỗng -> cả 2 bị chặn ngay ở model validation.
  assertHttp400(await a.upsertComment({ taskId: task.id, content: '' }), '#3 content ""');
  assertHttp400(await a.upsertComment({ taskId: task.id, content: '   ' }), '#3 khoảng trắng');
});

test('Flow05#4/#9: GET lọc type CSV; type lạ 400 (#4.1); task không tồn tại 404 (#9)', async () => {
  const t = await a.createTask(project.id);
  for (const type of ['comment', 'devlog', 'decision', 'track']) await a.addComment(t.id, { type });
  assert.deepEqual(ok(await a.getComments(t.id, 'devlog,decision')).map((c) => c.type).sort(), ['decision', 'devlog']);
  assertFail(await a.getComments(t.id, 'devlog,foo'), 400, null, '#4.1');
  assertFail(await a.getComments(MISSING_ID), 404, null, '#9');
});

test('Flow05#5: occurredAt có offset lưu đúng instant (#5.1), không offset -> HTTP 400 (#5.2), sắp theo occurredAt ?? createdAt (#5.3)', async () => {
  const t = await a.createTask(project.id);
  const now = await a.addComment(t.id, { content: 'bây giờ' });
  const occurredAt = '2026-01-01T02:00:00Z';
  const backdated = await a.addComment(t.id, { content: 'ghi lùi', type: 'devlog', occurredAt });
  assert.equal(Date.parse(backdated.occurredAt), Date.parse(occurredAt), '#5.1');
  assert.match(backdated.occurredAt, ISO_WITH_OFFSET, '#5.1');

  assert.equal((await a.upsertComment({ taskId: t.id, content: 'x', occurredAt: '2026-10-01T09:00:00' })).http, 400, '#5.2');

  assert.deepEqual(ok(await a.getComments(t.id)).map((c) => c.id), [backdated.id, now.id], '#5.3');
});
