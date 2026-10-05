/*
**Fullpath:** /api-tests/flows/01-project/01-project.test.js
*/
'use strict';

// Flow 01 — Project API. Xem testcase.md cùng folder (số # trong tên test = số dòng case).

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const { RUN_ID, request, createUser, ok } = require('../../_lib/api');

let a;

before(async () => {
  a = await createUser('p01');
});

after(async () => {
  await a?.cleanup();
});

test('Flow01#1: tạo project mới -> id>0, userId = user trong token, tự tạo workspace (#1.1, #1.2)', async () => {
  const res = await a.upsertProjects([{ id: 0, userId: 999999, name: `APITEST ${RUN_ID} create`, status: 'active' }]);
  const [p] = ok(res, 'create');
  assert.ok(p.id > 0, `id phải > 0, thực tế: ${p.id}`);
  assert.equal(p.userId, a.id, `userId phải là user trong token (${a.id}), không phải 999999 client gửi`);
  assert.ok(p.workspaceId > 0, `workspaceId phải được tự tạo, thực tế: ${p.workspaceId}`);
});

test('Flow01#2.1: thiếu name -> HTTP 400', async () => {
  const res = await a.call('POST', '/api/project', [{ id: 0, status: 'active' }]);
  assert.equal(res.http, 400);
});

test('Flow01#2.2: name > 255 ký tự -> HTTP 400', async () => {
  const res = await a.call('POST', '/api/project', [{ id: 0, name: 'x'.repeat(256) }]);
  assert.equal(res.http, 400);
});

test('Flow01#2.3: tạo mới kèm deletedAt -> 400 "Cannot set deletedAt on a new project"', async () => {
  const res = await a.call('POST', '/api/project', [{ id: 0, name: `APITEST ${RUN_ID} del-new`, deletedAt: new Date().toISOString() }]);
  assert.equal(res.body?.success, false);
  assert.equal(res.body?.status, 400);
  assert.match(res.body?.message ?? '', /deletedAt/);
});

test('Flow01#2.4: body rỗng [] -> HTTP 400', async () => {
  const res = await a.call('POST', '/api/project', []);
  assert.equal(res.http, 400);
});

test('Flow01#2.5: không có token -> HTTP 401', async () => {
  const res = await request('GET', '/api/project');
  assert.equal(res.http, 401);
});

test('Flow01#3: update name/description/status/startDate/endDate -> GET trả giá trị mới, ngày YYYY-MM-DD', async () => {
  const p = await a.createProject({ name: `APITEST ${RUN_ID} upd-before` });
  ok(await a.upsertProjects([{
    id: p.id, name: `APITEST ${RUN_ID} upd-after`, description: 'mô tả mới', status: 'paused',
    startDate: '2026-10-01', endDate: '2026-10-31', workspaceId: p.workspaceId,
  }]), 'update');

  const [got] = ok(await a.getProject(p.id), 'get');
  assert.equal(got.name, `APITEST ${RUN_ID} upd-after`);
  assert.equal(got.description, 'mô tả mới');
  assert.equal(got.status, 'paused');
  assert.equal(got.startDate, '2026-10-01');
  assert.equal(got.endDate, '2026-10-31');
});

test('Flow01#4: batch upsert 3 project trong 1 request -> trả đủ 3', async () => {
  const data = ok(await a.upsertProjects([1, 2, 3].map((i) => ({ id: 0, name: `APITEST ${RUN_ID} batch${i}`, status: 'active' }))), 'batch');
  assert.equal(data.length, 3);
  assert.ok(data.every((p) => p.id > 0 && p.userId === a.id));
});

test('Flow01#5.1/#5.2/#5.4: filter status CSV + ids; thứ tự createdAt giảm dần', async () => {
  const p1 = await a.createProject({ name: `APITEST ${RUN_ID} st-active`, status: 'active' });
  const p2 = await a.createProject({ name: `APITEST ${RUN_ID} st-paused`, status: 'paused' });
  const p3 = await a.createProject({ name: `APITEST ${RUN_ID} st-completed`, status: 'completed' });
  const ids = [p1.id, p2.id, p3.id].join(',');

  const byStatus = ok(await a.getProjects(`?ids=${ids}&status=active,paused`), 'filter');
  assert.deepEqual(byStatus.map((p) => p.id).sort(), [p1.id, p2.id].sort(), 'status=active,paused chỉ trả 2 project');

  const all = ok(await a.getProjects(`?ids=${ids}`), 'ids');
  assert.deepEqual(all.map((p) => p.id), [p3.id, p2.id, p1.id], 'ids filter trả đủ 3, mới tạo đứng trước');
});

test('Flow01#5.3: searchText khớp theo name và theo description', async () => {
  const token = `zq${RUN_ID}`;
  const byName = await a.createProject({ name: `APITEST ${token} by-name` });
  const byDesc = await a.createProject({ name: `APITEST ${RUN_ID} by-desc`, description: `có chứa ${token} trong mô tả` });
  await a.createProject({ name: `APITEST ${RUN_ID} no-match` });

  const found = ok(await a.getProjects(`?searchText=${token}`), 'search');
  assert.deepEqual(found.map((p) => p.id).sort(), [byName.id, byDesc.id].sort());
});

test('Flow01#6: GET /api/project/{id} trả đúng 1 project', async () => {
  const p = await a.createProject();
  const data = ok(await a.getProject(p.id), 'get by id');
  assert.equal(data.length, 1);
  assert.equal(data[0].id, p.id);
});

test('Flow01#7: soft delete rồi restore qua upsert deletedAt', async () => {
  const p = await a.createProject({ name: `APITEST ${RUN_ID} soft` });
  const base = { id: p.id, name: p.name, status: p.status, workspaceId: p.workspaceId };

  ok(await a.upsertProjects([{ ...base, deletedAt: new Date().toISOString() }]), 'soft delete');
  assert.equal(ok(await a.getProjects(`?ids=${p.id}&deletedAt=null`)).length, 0, '#7.1 deletedAt=null không được thấy project đã xoá');
  assert.equal(ok(await a.getProjects(`?ids=${p.id}&deletedAt=notNull`)).length, 1, '#7.1 deletedAt=notNull phải thấy');

  ok(await a.upsertProjects([{ ...base, deletedAt: null }]), 'restore');
  assert.equal(ok(await a.getProjects(`?ids=${p.id}&deletedAt=null`)).length, 1, '#7.2 restore xong phải thấy lại');
});

test('Flow01#8: status open/planned lưu & đọc lại được', async () => {
  for (const status of ['open', 'planned']) {
    const p = await a.createProject({ status });
    const [got] = ok(await a.getProject(p.id));
    assert.equal(got.status, status);
  }
});
