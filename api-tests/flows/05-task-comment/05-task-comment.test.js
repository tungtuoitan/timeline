/*
**Fullpath:** /api-tests/flows/05-task-comment/05-task-comment.test.js
*/
'use strict';

// Flow 05 — /api/taskcomment. Xem testcase.md cùng folder.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const { RUN_ID, createUser, ok } = require('../../_lib/api');

const MISSING_ID = 2147483000;
const ISO_WITH_OFFSET = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?([+-]\d{2}:\d{2}|Z)$/;

let a;
let project;
let task;

before(async () => {
  a = await createUser('c05');
  project = await a.createProject({ name: `APITEST ${RUN_ID} comments` });
  task = await a.createTask(project.id);
});

after(async () => {
  await a?.cleanup();
});

async function addComment(body) {
  const [c] = ok(await a.upsertComment({ taskId: task.id, ...body }), 'add comment');
  return c;
}

test('Flow05#1: tạo comment không gửi type -> type=comment, userId đúng, occurredAt null, createdAt có offset', async () => {
  const c = await addComment({ content: 'hello' });
  assert.ok(c.id > 0);
  assert.equal(c.type, 'comment');
  assert.equal(c.userId, a.id);
  assert.equal(c.occurredAt ?? null, null);
  assert.match(c.createdAt, ISO_WITH_OFFSET);
});

test('Flow05#1.1: message khi tạo mới là "Comment created"', async () => {
  const res = await a.upsertComment({ taskId: task.id, content: 'msg check' });
  ok(res);
  assert.equal(res.body.message, 'Comment created',
    'TaskCommentRepository.UpsertCommentAsync tính message SAU SaveChanges (Id đã > 0) nên luôn ra "Comment updated"');
});

test('Flow05#2: type devlog/decision/track lưu đúng; type lạ -> 400 (#2.1)', async () => {
  for (const type of ['devlog', 'decision', 'track']) {
    const c = await addComment({ content: `type ${type}`, type });
    assert.equal(c.type, type);
  }
  const res = await a.upsertComment({ taskId: task.id, content: 'x', type: 'foo' });
  assert.equal(res.body?.success, false);
  assert.equal(res.body?.status, 400);
  assert.match(res.body?.message ?? '', /Unknown comment type/);
});

test('Flow05#3: content rỗng / khoảng trắng -> bị từ chối', async () => {
  const empty = await a.upsertComment({ taskId: task.id, content: '' });
  assert.ok(empty.http === 400 || empty.body?.status === 400, `content "" phải 400, thực tế ${empty.http} ${JSON.stringify(empty.body)}`);
  const blank = await a.upsertComment({ taskId: task.id, content: '   ' });
  assert.ok(blank.http === 400 || blank.body?.status === 400, 'content khoảng trắng phải 400');
});

test('Flow05#4: GET lọc type=devlog,decision; type lạ -> 400 (#4.1)', async () => {
  const t = await a.createTask(project.id);
  for (const type of ['comment', 'devlog', 'decision', 'track']) {
    ok(await a.upsertComment({ taskId: t.id, content: type, type }));
  }
  const data = ok(await a.getComments(t.id, 'devlog,decision'));
  assert.deepEqual(data.map((c) => c.type).sort(), ['decision', 'devlog']);

  const bad = await a.getComments(t.id, 'devlog,foo');
  assert.equal(bad.body?.status, 400);
});

test('Flow05#5.1/#5.3: occurredAt có offset lưu đúng instant; GET sắp theo occurredAt ?? createdAt', async () => {
  const t = await a.createTask(project.id);
  const now = ok(await a.upsertComment({ taskId: t.id, content: 'bây giờ' }))[0];
  const occurredAt = '2026-01-01T02:00:00Z';
  const backdated = ok(await a.upsertComment({ taskId: t.id, content: 'ghi lùi', type: 'devlog', occurredAt }))[0];
  assert.equal(Date.parse(backdated.occurredAt), Date.parse(occurredAt), `occurredAt phải cùng instant, thực tế ${backdated.occurredAt}`);
  assert.match(backdated.occurredAt, ISO_WITH_OFFSET);

  const list = ok(await a.getComments(t.id));
  assert.deepEqual(list.map((c) => c.id), [backdated.id, now.id], 'comment backdate phải đứng trước');
});

test('Flow05#5.2: occurredAt không offset -> HTTP 400', async () => {
  const res = await a.upsertComment({ taskId: task.id, content: 'x', occurredAt: '2026-10-01T09:00:00' });
  assert.equal(res.http, 400);
});

test('Flow05#6.1: sửa content, không gửi type/occurredAt -> giữ nguyên type + occurredAt', async () => {
  const c = await addComment({ content: 'bản đầu', type: 'decision', occurredAt: '2026-09-30T03:00:00Z' });
  const [edited] = ok(await a.upsertComment({ id: c.id, taskId: task.id, content: 'bản sửa' }));
  assert.equal(edited.content, 'bản sửa');
  assert.equal(edited.type, 'decision');
  assert.equal(Date.parse(edited.occurredAt), Date.parse('2026-09-30T03:00:00Z'));
});

test('Flow05#6.2: sửa comment với taskId khác task của comment -> 404', async () => {
  const c = await addComment({ content: 'thuộc task gốc' });
  const other = await a.createTask(project.id);
  const res = await a.upsertComment({ id: c.id, taskId: other.id, content: 'sửa lệch task' });
  assert.equal(res.body?.status, 404);
});

test('Flow05#7/#8: reply cùng task OK (#7.1), cha khác task 404 (#7.2), xoá cha xoá luôn reply (#8)', async () => {
  const t = await a.createTask(project.id);
  const parent = ok(await a.upsertComment({ taskId: t.id, content: 'cha' }))[0];
  const reply = ok(await a.upsertComment({ taskId: t.id, content: 'con', parentCommentId: parent.id }))[0];
  assert.equal(reply.parentCommentId, parent.id);

  const cross = await a.upsertComment({ taskId: task.id, content: 'reply lệch task', parentCommentId: parent.id });
  assert.equal(cross.body?.status, 404);
  assert.match(cross.body?.message ?? '', /Parent comment/);

  ok(await a.deleteComment(parent.id), 'delete');
  const left = ok(await a.getComments(t.id));
  assert.equal(left.length, 0, 'cha và reply đều phải bị ẩn');

  const editDeleted = await a.upsertComment({ id: parent.id, taskId: t.id, content: 'sửa cái đã xoá' });
  assert.equal(editDeleted.body?.status, 404, '#6.3 sửa comment đã xoá -> 404');
});

test('Flow05#8.1: xoá comment không tồn tại -> 404', async () => {
  const res = await a.deleteComment(MISSING_ID);
  assert.equal(res.body?.success, false);
  assert.equal(res.body?.status, 404);
});

test('Flow05#9: GET comment của task không tồn tại -> 404', async () => {
  const res = await a.getComments(MISSING_ID);
  assert.equal(res.body?.status, 404);
});
