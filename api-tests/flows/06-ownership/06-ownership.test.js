/*
**Fullpath:** /api-tests/flows/06-ownership/06-ownership.test.js
*/
'use strict';

// Flow 06 — Ownership: user B không đụng được dữ liệu user A. Xem testcase.md cùng folder.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const { RUN_ID, createUser, ok } = require('../../_lib/api');

let a; // chủ dữ liệu
let b; // kẻ "tò mò"
const ctx = {};

function assertDenied(res, label) {
  assert.equal(res.body?.success, false, `${label}: phải bị từ chối, thực tế ${res.http} ${JSON.stringify(res.body)}`);
  assert.equal(res.body?.status, 404, `${label}: phải trả status 404 (không lộ tồn tại)`);
}

before(async () => {
  a = await createUser('o06a');
  b = await createUser('o06b');
  ctx.aProject = await a.createProject({ name: `APITEST ${RUN_ID} owner-A secret` });
  ctx.aTask = await a.createTask(ctx.aProject.id, { title: `APITEST ${RUN_ID} A task`, status: 'open' });
  ctx.aComment = ok(await a.upsertComment({ taskId: ctx.aTask.id, content: 'của A' }))[0];
  ctx.bProject = await b.createProject({ name: `APITEST ${RUN_ID} owner-B` });
  ctx.bTask = await b.createTask(ctx.bProject.id);
});

after(async () => {
  await a?.cleanup();
  await b?.cleanup();
});

async function assertATaskUnchanged() {
  const t = await a.getTask(ctx.aTask.id);
  assert.ok(t, 'task A phải còn');
  assert.equal(t.title, `APITEST ${RUN_ID} A task`);
  assert.equal(t.status, 'open');
  assert.equal(t.projectId, ctx.aProject.id);
}

test('Flow06#1: B đọc project của A -> rỗng', async () => {
  assert.equal(ok(await b.getProjects(`?ids=${ctx.aProject.id}`)).length, 0);
  assert.equal(ok(await b.getProject(ctx.aProject.id)).length, 0);
  assert.equal(ok(await b.getProjects(`?searchText=${encodeURIComponent('owner-A secret')}`)).length, 0);
});

test('Flow06#2: B đọc task của A -> rỗng', async () => {
  assert.equal(ok(await b.getTasks(`?projectIds=${ctx.aProject.id}`)).length, 0);
  assert.equal(ok(await b.getTasks(`?ids=${ctx.aTask.id}`)).length, 0);
  assert.equal(await b.getTask(ctx.aTask.id), undefined);
});

test('Flow06#3: B sửa project của A -> 404, project A không đổi; gắn workspace của A -> 404 (#3.1)', async () => {
  assertDenied(await b.upsertProjects([{ id: ctx.aProject.id, name: 'B hijack', status: 'dropped' }]), '#3');
  const [p] = ok(await a.getProject(ctx.aProject.id));
  assert.equal(p.name, `APITEST ${RUN_ID} owner-A secret`);
  assert.equal(p.userId, a.id);

  assertDenied(await b.upsertProjects([{ id: 0, name: `APITEST ${RUN_ID} B steals ws`, workspaceId: ctx.aProject.workspaceId }]), '#3.1');
});

test('Flow06#4: B tạo task vào project của A -> 404, không có task nào được tạo', async () => {
  const title = `APITEST ${RUN_ID} B into A`;
  assertDenied(await b.upsertTasks([{ id: 0, projectId: ctx.aProject.id, title }]), '#4');
  const found = ok(await a.getTasks(`?projectIds=${ctx.aProject.id}&searchText=${encodeURIComponent(title)}`));
  assert.equal(found.length, 0);
});

test('Flow06#5: B upsert task id của A (đặt vào project B) -> 404, task A không đổi', async () => {
  assertDenied(await b.upsertTasks([{ id: ctx.aTask.id, projectId: ctx.bProject.id, title: 'B hijack', status: 'completed' }]), '#5');
  await assertATaskUnchanged();
});

test('Flow06#6: B tạo task con của task A -> 404', async () => {
  assertDenied(await b.upsertTasks([{ id: 0, projectId: ctx.bProject.id, title: 'x', parentTaskId: ctx.aTask.id }]), '#6');
});

test('Flow06#7: B PATCH task của A -> HTTP 404; chuyển task B sang project A (#7.1) / gán cha task A (#7.2) -> 404', async () => {
  const res = await b.patchTask(ctx.aTask.id, { status: 'completed', title: 'B hijack' });
  assert.equal(res.http, 404);
  await assertATaskUnchanged();

  assert.equal((await b.patchTask(ctx.bTask.id, { projectId: ctx.aProject.id })).http, 404, '#7.1');
  assert.equal((await b.patchTask(ctx.bTask.id, { parentTaskId: ctx.aTask.id })).http, 404, '#7.2');
  const bt = await b.getTask(ctx.bTask.id);
  assert.equal(bt.projectId, ctx.bProject.id);
  assert.equal(bt.parentTaskId, null);
});

test('Flow06#8: B hard-delete task của A -> 404, task A còn', async () => {
  assertDenied(await b.hardDeleteTasks([ctx.aTask.id]), '#8');
  await assertATaskUnchanged();
});

test('Flow06#9: comment — B đọc (#9.1), comment (#9.2), sửa (#9.3), xoá (#9.4) trên task A đều bị chặn', async () => {
  assertDenied(await b.getComments(ctx.aTask.id), '#9.1');
  assertDenied(await b.upsertComment({ taskId: ctx.aTask.id, content: 'B chen vào' }), '#9.2');
  assertDenied(await b.upsertComment({ id: ctx.aComment.id, taskId: ctx.aTask.id, content: 'B sửa' }), '#9.3');

  const del = await b.deleteComment(ctx.aComment.id);
  assert.equal(del.body?.success, false, '#9.4 B xoá comment A phải thất bại');

  const list = ok(await a.getComments(ctx.aTask.id));
  const c = list.find((x) => x.id === ctx.aComment.id);
  assert.ok(c, 'comment A phải còn');
  assert.equal(c.content, 'của A');
});
