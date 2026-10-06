/*
**Fullpath:** /api-tests/flows/09-totp/09-1-setup-unlock.test.js
*/
'use strict';

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { totpCode, currentStep } = require('../../_lib/totp');

let a;
let secret;
let confirmStep;
let confirmCode;

before(async () => {
  a = await dm.newSession('09-1');
});

after(async () => {
  await a?.cleanup();
});

test('Flow09#1: user mới chưa bật, unlock -> 404', async () => {
  const st = await a.getTotpStatus();
  assert.equal(st.http, 200);
  assert.equal(st.body.object.enabled, false);
  assert.equal((await a.unlockTotp('123456')).http, 404);
});

test('Flow09#2: setup -> confirm sai 400 -> confirm đúng trả vé + enabled', async () => {
  const setup = await a.setupTotp();
  assert.equal(setup.http, 200);
  secret = setup.body.object.secret;
  assert.match(setup.body.object.otpauthUri, /^otpauth:\/\/totp\/SuperApp:.+\?secret=[A-Z2-7]+&issuer=SuperApp/);

  confirmStep = currentStep();
  const wrong = String((Number(totpCode(secret, confirmStep)) + 1) % 1e6).padStart(6, '0');
  assert.equal((await a.confirmTotp(wrong)).http, 400);

  confirmCode = totpCode(secret, confirmStep);
  const ok = await a.confirmTotp(confirmCode);
  assert.equal(ok.http, 200);
  assert.match(ok.body.object.token, /^\d+\.\d+\.[\w-]+$/);
  assert.equal((await a.getTotpStatus()).body.object.enabled, true);
});

test('Flow09#2.1: setup lại khi đã bật -> 409', async () => {
  assert.equal((await a.setupTotp()).http, 409);
});

test('Flow09#3: mã đã dùng -> 400; mã bước kế tiếp -> vé mới', async () => {
  assert.equal((await a.unlockTotp(confirmCode)).http, 400, 'mã chỉ dùng 1 lần');
  const next = await a.unlockTotp(totpCode(secret, confirmStep + 1));
  assert.equal(next.http, 200, JSON.stringify(next.body));
  assert.ok(next.body.object.token);
});

test('Flow09#7: tắt TOTP bằng mã đúng', async () => {
  // bước +1 đã dùng ở case 3 → cần bước +2: chờ sang bước kế tiếp của đồng hồ
  const target = confirmStep + 2;
  while (currentStep() + 1 < target) await new Promise((r) => setTimeout(r, 1000));
  const res = await a.disableTotp(totpCode(secret, target));
  assert.equal(res.http, 200, JSON.stringify(res.body));
  assert.equal((await a.getTotpStatus()).body.object.enabled, false);
  assert.equal((await a.unlockTotp(totpCode(secret, target))).http, 404);
});
