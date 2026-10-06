/*
**Fullpath:** /api-tests/flows/09-totp/09-3-private-apis.test.js
*/
'use strict';

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { ok } = require('../../_lib/verify');

let a;
let b;
let tokenA;
let tokenB;
let open; // tracker thường
let secret; // tracker nhạy cảm

const RANGE = '?from=2026-10-01&to=2026-10-06';

before(async () => {
  a = await dm.newSession('09-3a');
  b = await dm.newSession('09-3b');
  tokenA = (await a.enableTotp()).token;
  tokenB = (await b.enableTotp()).token;
  const p = await a.createProject();
  open = await a.createTask(p.id, { type: 'repeat', status: 'background_progress' });
  secret = await a.createTask(p.id, { type: 'repeat', status: 'background_progress' });
  ok(await a.patchTask(secret.id, { isSensitive: true }));
});

after(async () => {
  await a?.cleanup();
  await b?.cleanup();
});

const ids = async (query, token) => ok(await a.getHabits(query, token)).map((x) => x.taskId);

test('Flow09#5: habits bỏ tracker nhạy cảm khi không có vé hợp lệ', async () => {
  assert.deepEqual(await ids(RANGE), [open.id], 'không vé');
  assert.deepEqual((await ids(RANGE, tokenA)).sort((x, y) => x - y), [open.id, secret.id].sort((x, y) => x - y), 'vé của A');
  assert.deepEqual(await ids(RANGE, tokenB), [open.id], 'vé của user khác');
  assert.deepEqual(await ids(RANGE, 'garbage'), [open.id], 'vé rác');
  assert.deepEqual(await ids(`${RANGE}&taskIds=${secret.id}`), [], 'gọi thẳng taskIds cũng không lộ');
});

test('Flow09#5.1: isSensitive giữ nguyên sau upsert (full replace)', async () => {
  const [t] = ok(await a.getTasks(`?ids=${secret.id}`));
  assert.equal(t.isSensitive, true);
  ok(await a.upsertTasks([{ id: secret.id, projectId: t.projectId, title: 'đổi tên', type: 'repeat', status: 'background_progress' }]));
  const [after1] = ok(await a.getTasks(`?ids=${secret.id}`));
  assert.equal(after1.title, 'đổi tên');
  assert.equal(after1.isSensitive, true, 'upsert không xoá cờ');
});

test('Flow09#6: finance summary cần vé', async () => {
  assert.equal((await a.getFinSummary('?from=2026-10-01&to=2026-10-06')).http, 403);
  assert.equal((await a.getFinSummary('?from=2026-10-01&to=2026-10-06', tokenB)).http, 403, 'vé user khác');
  assert.equal((await a.getFinSummary('?from=2026-10-01&to=2026-10-06', tokenA)).http, 200);
});
