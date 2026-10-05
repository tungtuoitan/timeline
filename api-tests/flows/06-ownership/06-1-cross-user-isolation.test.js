/*
**Fullpath:** /api-tests/flows/06-ownership/06-1-cross-user-isolation.test.js
*/
'use strict';

// Flow 06 — user B không đọc/sửa/xoá được dữ liệu user A. Case #1–#9.
// Ghi bị chặn trả body.status 404 (OwnershipGuard — không lộ việc row tồn tại); đọc trả rỗng.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { RUN_ID } = require('../../_lib/config');
const { ok, assertDenied } = require('../../_lib/verify');

let a; // chủ dữ liệu
let b; // user khác
const ctx = {};
const A_TITLE = `APITEST ${RUN_ID} A task`;

before(async () => {
  a = await dm.newSession('06-1a');
  b = await dm.newSession('06-1b');
  ctx.aProject = await a.createProject({ name: `APITEST ${RUN_ID} owner-A secret` });
  ctx.aTask = await a.createTask(ctx.aProject.id, { title: A_TITLE, status: 'open' });
  ctx.aComment = await a.addComment(ctx.aTask.id, { content: 'của A' });
  ctx.bProject = await b.createProject();
  ctx.bTask = await b.createTask(ctx.bProject.id);
});

after(async () => {
  await a?.cleanup();
  await b?.cleanup();
});

async function assertATaskUnchanged() {
  const t = await a.getTask(ctx.aTask.id);
  assert.ok(t, 'task A phải còn');
  assert.equal(t.title, A_TITLE);
  assert.equal(t.status, 'open');
  assert.equal(t.projectId, ctx.aProject.id);
}

test('Flow06#1/#2: B đọc project/task của A -> rỗng', async () => {
  assert.equal(ok(await b.getProjects(`?ids=${ctx.aProject.id}`)).length, 0, '#1 ids');
  assert.equal(ok(await b.getProjectRes(ctx.aProject.id)).length, 0, '#1 by id');
  assert.equal(ok(await b.getProjects(`?searchText=${encodeURIComponent('owner-A secret')}`)).length, 0, '#1 search');
  assert.equal(ok(await b.getTasks(`?projectIds=${ctx.aProject.id}`)).length, 0, '#2 projectIds');
  assert.equal(ok(await b.getTasks(`?ids=${ctx.aTask.id}`)).length, 0, '#2 ids');
  assert.equal(await b.getTask(ctx.aTask.id), undefined, '#2 by id');
});

test('Flow06#3: B sửa project A -> 404 và A không đổi; B gắn workspace của A (#3.1) -> 404', async () => {
  assertDenied(await b.upsertProjects([{ id: ctx.aProject.id, name: 'B hijack', status: 'dropped' }]), '#3');
  const [p] = ok(await a.getProjectRes(ctx.aProject.id));
  assert.equal(p.name, `APITEST ${RUN_ID} owner-A secret`);
  assert.equal(p.userId, a.id);
  assertDenied(await b.upsertProjects([{ id: 0, name: 'B steals ws', workspaceId: ctx.aProject.workspaceId }]), '#3.1');
});

test('Flow06#4/#5/#6: B tạo task vào project A, ghi đè task A, tạo con của task A -> 404, A không đổi', async () => {
  const title = `APITEST ${RUN_ID} B into A`;
  assertDenied(await b.upsertTasks([{ id: 0, projectId: ctx.aProject.id, title }]), '#4');
  assert.equal(ok(await a.getTasks(`?projectIds=${ctx.aProject.id}&searchText=${encodeURIComponent(title)}`)).length, 0, '#4 không tạo gì');
  assertDenied(await b.upsertTasks([{ id: ctx.aTask.id, projectId: ctx.bProject.id, title: 'B hijack', status: 'completed' }]), '#5');
  assertDenied(await b.upsertTasks([{ id: 0, projectId: ctx.bProject.id, title: 'x', parentTaskId: ctx.aTask.id }]), '#6');
  await assertATaskUnchanged();
});

test('Flow06#7: B PATCH task A -> HTTP 404; B chuyển task mình sang project A (#7.1) / gán cha task A (#7.2) -> 404', async () => {
  assert.equal((await b.patchTask(ctx.aTask.id, { status: 'completed', title: 'B hijack' })).http, 404, '#7');
  await assertATaskUnchanged();
  assert.equal((await b.patchTask(ctx.bTask.id, { projectId: ctx.aProject.id })).http, 404, '#7.1');
  assert.equal((await b.patchTask(ctx.bTask.id, { parentTaskId: ctx.aTask.id })).http, 404, '#7.2');
  const bt = await b.getTask(ctx.bTask.id);
  assert.equal(bt.projectId, ctx.bProject.id);
  assert.equal(bt.parentTaskId, null);
});

test('Flow06#8: B hard-delete task A -> 404, task A còn', async () => {
  assertDenied(await b.hardDeleteTasks([ctx.aTask.id]));
  await assertATaskUnchanged();
});

test('Flow06#9: B đọc (#9.1) / comment (#9.2) / sửa (#9.3) / xoá (#9.4) comment trên task A -> bị chặn, comment A nguyên vẹn', async () => {
  assertDenied(await b.getComments(ctx.aTask.id), '#9.1');
  assertDenied(await b.upsertComment({ taskId: ctx.aTask.id, content: 'B chen vào' }), '#9.2');
  assertDenied(await b.upsertComment({ id: ctx.aComment.id, taskId: ctx.aTask.id, content: 'B sửa' }), '#9.3');
  assert.equal((await b.deleteComment(ctx.aComment.id)).body?.success, false, '#9.4');
  const c = ok(await a.getComments(ctx.aTask.id)).find((x) => x.id === ctx.aComment.id);
  assert.ok(c, 'comment A phải còn');
  assert.equal(c.content, 'của A');
});
