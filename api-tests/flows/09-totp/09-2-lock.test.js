/*
**Fullpath:** /api-tests/flows/09-totp/09-2-lock.test.js
*/
'use strict';

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { totpCode, currentStep } = require('../../_lib/totp');

let a;
let secret;

before(async () => {
  a = await dm.newSession('09-2');
  ({ secret } = await a.enableTotp());
});

after(async () => {
  await a?.cleanup();
});

test('Flow09#4: sai 3 lần -> khoá 10 phút; đang khoá thì mã đúng cũng bị từ chối', async () => {
  const wrong = String((Number(totpCode(secret)) + 7) % 1e6).padStart(6, '0');
  assert.equal((await a.unlockTotp(wrong)).http, 400, 'sai lần 1');
  assert.equal((await a.unlockTotp(wrong)).http, 400, 'sai lần 2');
  const third = await a.unlockTotp(wrong);
  assert.equal(third.http, 423, 'sai lần 3 -> khoá');
  const minutes = (Date.parse(third.body.object.lockedUntil) - Date.now()) / 60000;
  assert.ok(minutes > 9 && minutes <= 10.1, `khoá ~10 phút, thực tế ${minutes}`);

  const correct = await a.unlockTotp(totpCode(secret, currentStep() + 1));
  assert.equal(correct.http, 423, 'đang khoá -> không xét mã');
  assert.ok((await a.getTotpStatus()).body.object.lockedUntil, 'status báo đang khoá');
});
