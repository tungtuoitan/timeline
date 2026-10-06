/*
**Fullpath:** /api-tests/_lib/totp.js
*/
'use strict';

/**
 * totp.js — `/api/security/totp/*` (TotpController, TungRoot #1489) + tính mã TOTP như app Authenticator.
 * Lỗi trả HTTP thật: 400 sai mã, 404 chưa cài/chưa bật, 409 đã bật, 423 đang khoá (sai 3 lần → 10 phút,
 * sai 5 lần → 1 ngày). Vé mở khoá gửi ở header `X-Unlock-Token`, sống 15 phút.
 */

const crypto = require('node:crypto');

const B32 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';

function base32Decode(s) {
  let bits = 0;
  let value = 0;
  const out = [];
  for (const c of s.replace(/=+$/, '').toUpperCase()) {
    value = (value << 5) | B32.indexOf(c);
    bits += 5;
    if (bits >= 8) {
      out.push((value >>> (bits - 8)) & 0xff);
      bits -= 8;
    }
  }
  return Buffer.from(out);
}

/** Bước thời gian 30 giây hiện tại (+ offset bước). */
function currentStep(offset = 0) {
  return Math.floor(Date.now() / 1000 / 30) + offset;
}

/** Mã 6 số của secret base32 ở bước `step` (RFC 6238, SHA-1). */
function totpCode(secret, step = currentStep()) {
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(step));
  const h = crypto.createHmac('sha1', base32Decode(secret)).update(counter).digest();
  const o = h[h.length - 1] & 0x0f;
  const bin = ((h[o] & 0x7f) << 24) | (h[o + 1] << 16) | (h[o + 2] << 8) | h[o + 3];
  return String(bin % 1e6).padStart(6, '0');
}

const getTotpStatus = (s) => s.call('GET', '/api/security/totp/status');
const setupTotp = (s) => s.call('POST', '/api/security/totp/setup');
const confirmTotp = (s, code) => s.call('POST', '/api/security/totp/confirm', { code });
const unlockTotp = (s, code) => s.call('POST', '/api/security/totp/unlock', { code });
const disableTotp = (s, code) => s.call('POST', '/api/security/totp/disable', { code });

/**
 * Cài + bật TOTP cho user test, throw nếu fail. Mã xác nhận dùng bước hiện tại.
 * @returns {Promise<{secret:string, token:string, step:number}>} token = vé mở khoá (confirm trả luôn).
 */
async function enableTotp(s) {
  const setup = await setupTotp(s);
  const secret = setup.body?.object?.secret;
  if (!secret) throw new Error(`setupTotp fail: ${JSON.stringify(setup.body)}`);
  const step = currentStep();
  const res = await confirmTotp(s, totpCode(secret, step));
  if (!res.body?.success) throw new Error(`confirmTotp fail: ${JSON.stringify(res.body)}`);
  return { secret, token: res.body.object.token, step };
}

module.exports = { totpCode, currentStep, getTotpStatus, setupTotp, confirmTotp, unlockTotp, disableTotp, enableTotp };
