/*
**Fullpath:** /api-tests/_lib/config.js
*/
'use strict';

/**
 * config.js — cấu hình dùng chung cho mọi module `_lib/` và case.
 *
 * - BASE_URL: BE cần test. Mặc định BE local `http://localhost:5000` (đang trỏ SuperApp-dev).
 *   Đổi qua env `SA_API_URL`. Host không phải localhost bị CHẶN (tránh signup + tạo rác trên
 *   prod tungle.uk) trừ khi set `SA_API_ALLOW_REMOTE=1`.
 * - TEST_EMAIL_PREFIX / TEST_EMAIL_DOMAIN: mọi user test có email
 *   `apitest+<run>-<tag>@test.local`. Đây là "prefix" nhận diện data test — script
 *   `_scripts/cleanup-test-data-by-prefix.sql` dọn theo đúng pattern này, nên ĐỪNG đổi một bên mà
 *   quên bên kia.
 * - RUN_ID: duy nhất mỗi process (file test) -> email không trùng giữa các file/lần chạy.
 */

const crypto = require('node:crypto');

const BASE_URL = (process.env.SA_API_URL || 'http://localhost:5000').replace(/\/$/, '');

if (!/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(BASE_URL) && process.env.SA_API_ALLOW_REMOTE !== '1') {
  throw new Error(`api-tests chỉ chạy với BE local (dev DB). SA_API_URL=${BASE_URL} — set SA_API_ALLOW_REMOTE=1 nếu thật sự muốn.`);
}

module.exports = {
  BASE_URL,
  TEST_EMAIL_PREFIX: 'apitest+',
  TEST_EMAIL_DOMAIN: 'test.local',
  RUN_ID: `${Date.now().toString(36)}${crypto.randomBytes(2).toString('hex')}`,
  /** Id chắc chắn không tồn tại — dùng cho case "không tìm thấy". */
  MISSING_ID: 2147483000,
};
