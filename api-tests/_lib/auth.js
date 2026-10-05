/*
**Fullpath:** /api-tests/_lib/auth.js
*/
'use strict';

const crypto = require('node:crypto');
const { RUN_ID, TEST_EMAIL_PREFIX, TEST_EMAIL_DOMAIN } = require('./config');
const { request } = require('./http');

/**
 * Signup 1 user test MỚI qua `POST /api/auth/signup` (local auth).
 *
 * Mỗi file test tạo user riêng -> data của các file cô lập hoàn toàn, chạy song song cũng
 * không va chạm (khác #113, nơi mọi case dùng chung data DEV nên bị va chạm khi chạy cả bộ).
 *
 * Preconditions: BE chạy, DB kết nối được. Rate limit signup 10 req/s/IP — run-tests.js chạy
 *   tuần tự nên không chạm ngưỡng.
 * @param {string} tag  hậu tố dễ đọc trong email, vd 'p01' -> `apitest+<run>-p01@test.local`.
 * @returns {Promise<{id:number, email:string, token:string}>}
 * Side-effects: tạo urm.users + refresh token. API không có endpoint xoá user — user (và data
 *   của nó) chỉ dọn được bằng `_scripts/cleanup-test-data-by-prefix.sql`.
 *   Password sinh ngẫu nhiên, chỉ nằm trong RAM, không log.
 */
async function signupUser(tag) {
  const email = `${TEST_EMAIL_PREFIX}${RUN_ID}-${tag}@${TEST_EMAIL_DOMAIN}`;
  const password = `${crypto.randomBytes(12).toString('base64url')}A1!`;
  const res = await request('POST', '/api/auth/signup', { form: { email, password } });
  const token = res.body?.user?.token;
  if (res.http !== 200 || !token) {
    throw new Error(`Signup ${email} thất bại: HTTP ${res.http} ${JSON.stringify(res.body)}`);
  }
  return { id: res.body.user.id, email, token };
}

module.exports = { signupUser };
