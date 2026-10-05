/*
**Fullpath:** /api-tests/flows/01-project/01-1-create-validate.test.js
*/
'use strict';

// Flow 01 nhóm 1 — tạo project + validate input. Case #1, #2.x trong testcase.md.

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { RUN_ID } = require('../../_lib/config');
const { ok, assertFail, assertHttp400 } = require('../../_lib/verify');

let a;

before(async () => { a = await dm.newSession('01-1'); });
after(async () => { await a?.cleanup(); });

test('Flow01#1: tạo project -> id>0, userId lấy từ token (bỏ qua userId client gửi), workspace tự tạo', async () => {
  const [p] = ok(await a.upsertProjects([{ id: 0, userId: 999999, name: `APITEST ${RUN_ID} create`, status: 'active' }]));
  assert.ok(p.id > 0);
  assert.equal(p.userId, a.id, '#1.2 userId phải là user trong token');
  assert.ok(p.workspaceId > 0, '#1.1 workspace phải được tự tạo');
});

test('Flow01#2.1/#2.2/#2.4: thiếu name, name > 255, body [] -> HTTP 400', async () => {
  assertHttp400(await a.call('POST', '/api/project', [{ id: 0, status: 'active' }]), '#2.1');
  assertHttp400(await a.call('POST', '/api/project', [{ id: 0, name: 'x'.repeat(256) }]), '#2.2');
  assertHttp400(await a.call('POST', '/api/project', []), '#2.4');
});

test('Flow01#2.3: tạo mới kèm deletedAt -> body.status 400', async () => {
  assertFail(await a.upsertProjects([{ id: 0, name: 'x', deletedAt: new Date().toISOString() }]), 400, /deletedAt/);
});

test('Flow01#2.5: không có token -> HTTP 401', async () => {
  assert.equal((await dm.request('GET', '/api/project')).http, 401);
});
